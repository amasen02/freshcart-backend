#!/usr/bin/env bash
set -euo pipefail

required_commands=(curl docker dotnet npm setsid)

preflight() {
  [[ "${CI:-}" == true ]] || { echo "This helper is restricted to an isolated CI Docker host" >&2; return 1; }
  local missing=()
  for command_name in "${required_commands[@]}"; do
    command -v "$command_name" >/dev/null 2>&1 || missing+=("$command_name")
  done
  if ((${#missing[@]})); then
    echo "Missing required commands: ${missing[*]}" >&2
    return 1
  fi
  docker info >/dev/null
}

snapshot_docker() {
  local state_dir=$1
  mkdir -p "$state_dir"
  docker ps -aq | sort -u > "$state_dir/containers.before"
  docker volume ls -q | sort -u > "$state_dir/volumes.before"
  docker network ls -q | sort -u > "$state_dir/networks.before"
}

start_stack() {
  local state_dir=$1 backend_dir=$2 frontend_dir=$3
  mkdir -p "$state_dir"
  snapshot_docker "$state_dir"
  (cd "$backend_dir" && exec setsid env FreshCart__Ephemeral=true dotnet run --project src/AspireAppHost/FreshCart.AppHost/FreshCart.AppHost.csproj --launch-profile http) > "$state_dir/apphost.log" 2>&1 &
  echo $! > "$state_dir/apphost.pid"
  (cd "$frontend_dir" && exec setsid npm start -- --host 127.0.0.1 --port 4200) > "$state_dir/storefront.log" 2>&1 &
  echo $! > "$state_dir/storefront.pid"
}

wait_for_url() {
  local name=$1 url=$2 timeout_seconds=$3 state_dir=$4 curl_insecure=${5:-false}
  local deadline=$((SECONDS + timeout_seconds)) curl_args=(--fail --silent --show-error --max-time 5)
  [[ "$curl_insecure" == true ]] && curl_args+=(--insecure)
  while ((SECONDS < deadline)); do
    for pid_file in "$state_dir/apphost.pid" "$state_dir/storefront.pid"; do
      if [[ -f "$pid_file" ]] && ! kill -0 "$(<"$pid_file")" 2>/dev/null; then
        echo "A stack process exited while waiting for $name" >&2
        tail -n 80 "$state_dir/apphost.log" "$state_dir/storefront.log" 2>/dev/null || true
        return 1
      fi
    done
    if curl "${curl_args[@]}" "$url" >/dev/null 2>&1; then echo "ready: $name ($url)"; return 0; fi
    sleep 2
  done
  echo "Timed out after ${timeout_seconds}s waiting for $name ($url)" >&2
  tail -n 80 "$state_dir/apphost.log" "$state_dir/storefront.log" 2>/dev/null || true
  return 1
}

wait_for_stack() {
  local state_dir=$1 timeout_seconds=${2:-600}
  local deadline=$((SECONDS + timeout_seconds))
  wait_one() {
    local remaining=$((deadline - SECONDS))
    ((remaining > 0)) || { echo "Global stack readiness timeout expired" >&2; return 1; }
    wait_for_url "$1" "$2" "$remaining" "$state_dir" "${3:-false}"
  }
  wait_one identity http://localhost:5101/ready
  wait_one catalog http://localhost:5102/ready
  wait_one basket http://localhost:5104/ready
  wait_one ordering http://localhost:5105/ready
  wait_one payment http://localhost:5107/ready
  wait_one notification http://localhost:5109/ready
  wait_one reporting http://localhost:5110/ready
  wait_one gateway https://localhost:7100/ready true
  wait_one storefront http://localhost:4200
}

new_docker_ids() {
  local kind=$1 before_file=$2
  case "$kind" in containers) docker ps -aq ;; volumes) docker volume ls -q ;; networks) docker network ls -q ;; esac | sort -u | comm -13 "$before_file" -
}

collect_diagnostics() {
  local state_dir=$1 diagnostics_dir=$1/diagnostics
  mkdir -p "$diagnostics_dir"
  for snapshot in containers.before volumes.before networks.before; do
    if [[ ! -f "$state_dir/$snapshot" ]]; then
      echo "Docker ownership snapshot unavailable; no Docker diagnostics collected." > "$diagnostics_dir/README.txt"
      return 0
    fi
  done

  : > "$diagnostics_dir/containers.txt"
  mapfile -t containers < <(new_docker_ids containers "$state_dir/containers.before")
  for container in "${containers[@]}"; do
    docker inspect --format '{{.Name}} status={{.State.Status}} health={{if .State.Health}}{{.State.Health.Status}}{{else}}none{{end}}' "$container" >> "$diagnostics_dir/containers.txt" 2>&1 || true
    docker logs --tail 80 "$container" > "$diagnostics_dir/container-$container.log" 2>&1 || true
  done

  : > "$diagnostics_dir/volumes.txt"
  mapfile -t volumes < <(new_docker_ids volumes "$state_dir/volumes.before")
  for volume in "${volumes[@]}"; do
    docker volume inspect --format '{{.Name}} driver={{.Driver}}' "$volume" >> "$diagnostics_dir/volumes.txt" 2>&1 || true
  done

  : > "$diagnostics_dir/networks.txt"
  mapfile -t networks < <(new_docker_ids networks "$state_dir/networks.before")
  for network in "${networks[@]}"; do
    docker network inspect --format '{{.Name}} driver={{.Driver}}' "$network" >> "$diagnostics_dir/networks.txt" 2>&1 || true
  done
  echo "Collected bounded diagnostics for ${#containers[@]} containers, ${#volumes[@]} volumes, and ${#networks[@]} networks." > "$diagnostics_dir/README.txt"
}

cleanup() {
  local state_dir=$1 pid
  [[ "${CI:-}" == true ]] || { echo "Cleanup is restricted to an isolated CI Docker host" >&2; return 1; }
  for snapshot in containers.before volumes.before networks.before; do
    [[ -f "$state_dir/$snapshot" ]] || { echo "Ownership snapshot is incomplete; refusing Docker cleanup" >&2; return 1; }
  done
  for pid_file in "$state_dir/storefront.pid" "$state_dir/apphost.pid"; do
    if [[ -f "$pid_file" ]]; then
      pid=$(<"$pid_file")
      kill -- "-$pid" 2>/dev/null || true
      for _ in {1..20}; do kill -0 -- "-$pid" 2>/dev/null || break; sleep 0.5; done
      if kill -0 -- "-$pid" 2>/dev/null; then
        kill -KILL -- "-$pid" 2>/dev/null || true
        for _ in {1..10}; do kill -0 -- "-$pid" 2>/dev/null || break; sleep 0.2; done
      fi
      kill -0 -- "-$pid" 2>/dev/null && { echo "Process group $pid survived cleanup" >&2; return 1; }
    fi
  done
  mapfile -t containers < <(new_docker_ids containers "$state_dir/containers.before")
  ((${#containers[@]} == 0)) || docker rm -f "${containers[@]}"
  mapfile -t volumes < <(new_docker_ids volumes "$state_dir/volumes.before")
  ((${#volumes[@]} == 0)) || docker volume rm "${volumes[@]}"
  mapfile -t networks < <(new_docker_ids networks "$state_dir/networks.before")
  ((${#networks[@]} == 0)) || docker network rm "${networks[@]}"
  [[ -z "$(new_docker_ids containers "$state_dir/containers.before")$(new_docker_ids volumes "$state_dir/volumes.before")$(new_docker_ids networks "$state_dir/networks.before")" ]] || { echo "FreshCart-owned Docker resources remain after cleanup" >&2; return 1; }
}

if [[ "${FRESHCART_STACK_LIB_ONLY:-}" != 1 ]]; then
  command_name=${1:-}; shift || true
  case "$command_name" in preflight) preflight "$@" ;; start) start_stack "$@" ;; wait) wait_for_stack "$@" ;; diagnostics) collect_diagnostics "$@" ;; cleanup) cleanup "$@" ;; *) echo "Usage: $0 {preflight|start|wait|diagnostics|cleanup}" >&2; exit 2 ;; esac
fi
