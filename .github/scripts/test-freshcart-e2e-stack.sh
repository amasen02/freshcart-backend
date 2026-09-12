#!/usr/bin/env bash
set -euo pipefail
script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
export FRESHCART_STACK_LIB_ONLY=1
source "$script_dir/freshcart-e2e-stack.sh"
tmp_dir=$(mktemp -d)
trap 'rm -rf "$tmp_dir"' EXIT
export CI=true

original_path=$PATH; PATH=$tmp_dir; unset CI
if preflight >/dev/null 2>&1; then echo "preflight passed without prerequisites" >&2; exit 1; fi
PATH=$original_path; export CI=true

(exit 7) & dead_pid=$!; wait "$dead_pid" 2>/dev/null || true
echo "$dead_pid" > "$tmp_dir/apphost.pid"; : > "$tmp_dir/apphost.log"; : > "$tmp_dir/storefront.log"
if wait_for_url dead-service http://127.0.0.1:1 1 "$tmp_dir" >/dev/null 2>&1; then echo "dead process was ignored" >&2; exit 1; fi
rm "$tmp_dir/apphost.pid"

started=$SECONDS
if wait_for_url timeout-service http://127.0.0.1:1 1 "$tmp_dir" >/dev/null 2>&1; then echo "unreachable endpoint passed" >&2; exit 1; fi
((SECONDS - started >= 1)) || { echo "timeout was not honored" >&2; exit 1; }

# Cleanup may remove only resources created after the ownership snapshot.
state="$tmp_dir/cleanup"; mkdir -p "$state"
printf 'existing-container\n' > "$state/containers.before"
printf 'existing-volume\n' > "$state/volumes.before"
printf 'existing-network\n' > "$state/networks.before"
printf 'existing-container\nnew-container\n' > "$tmp_dir/containers.current"
printf 'existing-volume\nnew-volume\n' > "$tmp_dir/volumes.current"
printf 'existing-network\nnew-network\n' > "$tmp_dir/networks.current"
: > "$tmp_dir/removals"
setsid bash -c 'sleep 30 & wait' & owned_group=$!
echo "$owned_group" > "$state/storefront.pid"
docker() {
  if [[ "$1 $2" == 'ps -aq' ]]; then cat "$tmp_dir/containers.current"
  elif [[ "$1 $2 $3" == 'volume ls -q' ]]; then cat "$tmp_dir/volumes.current"
  elif [[ "$1 $2 $3" == 'network ls -q' ]]; then cat "$tmp_dir/networks.current"
  elif [[ "$1" == 'inspect' ]]; then printf 'inspect:%s\n' "${*:2}"
  elif [[ "$1 $2" == 'logs --tail' ]]; then printf 'bounded-log:%s\n' "${*:4}"
  elif [[ "$1 $2 $3" == 'volume inspect --format' ]]; then printf 'volume-inspect:%s\n' "${*:4}"
  elif [[ "$1 $2 $3" == 'network inspect --format' ]]; then printf 'network-inspect:%s\n' "${*:4}"
  elif [[ "$1 $2" == 'rm -f' ]]; then printf 'container:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/containers.before" "$tmp_dir/containers.current"
  elif [[ "$1 $2" == 'volume rm' ]]; then printf 'volume:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/volumes.before" "$tmp_dir/volumes.current"
  elif [[ "$1 $2" == 'network rm' ]]; then printf 'network:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/networks.before" "$tmp_dir/networks.current"
  else return 1
  fi
}
collect_diagnostics "$state"
grep -F 'new-container' "$state/diagnostics/containers.txt" >/dev/null
if grep -F 'existing-container' "$state/diagnostics/containers.txt" >/dev/null; then echo "diagnostics included a pre-existing container" >&2; exit 1; fi
[[ -f "$state/diagnostics/container-new-container.log" ]]
echo "bounded owned Docker diagnostics passed"
cleanup "$state"
wait "$owned_group" 2>/dev/null || true
if kill -0 -- "-$owned_group" 2>/dev/null; then echo "cleanup left its process group running" >&2; exit 1; fi
grep -Fx 'container:new-container' "$tmp_dir/removals"
grep -Fx 'volume:new-volume' "$tmp_dir/removals"
grep -Fx 'network:new-network' "$tmp_dir/removals"
if grep -F 'existing-' "$tmp_dir/removals"; then echo "cleanup removed a pre-existing resource" >&2; exit 1; fi

mongo_source="$script_dir/../../src/AspireAppHost/FreshCart.AppHost/Program.cs"
mongo_initializer="$script_dir/../../src/AspireAppHost/FreshCart.AppHost/mongo-replica-set-init.sh"
grep -F 'RunAsync().ConfigureAwait(false)' "$mongo_source" >/dev/null
grep -F 'Replace("\r\n", "\n"' "$mongo_source" >/dev/null
grep -F '&directConnection=true' "$mongo_source" >/dev/null
grep -F 'ResourceAnnotationMutationBehavior.Replace' "$mongo_source" >/dev/null
[[ $(grep -c 'RegisterMongoDatabaseHealthCheck' "$mongo_source") -eq 6 ]]
grep -F 'status.set && status.set !== "rs0"' "$mongo_initializer" >/dev/null
grep -F 'error.code === 94' "$mongo_initializer" >/dev/null
grep -F 'error.code === 76' "$mongo_initializer" >/dev/null
grep -F 'rs.initiate' "$mongo_initializer" >/dev/null
grep -F 'FRESHCART_MONGO_INIT_LIB_ONLY' "$mongo_initializer" >/dev/null
if grep -n $'\r' "$mongo_initializer" >/dev/null; then
  echo "Mongo initializer must use LF line endings" >&2
  exit 1
fi
echo "production source sanity checks passed"

fake_mongosh="$tmp_dir/mongosh"
cat > "$fake_mongosh" <<'FAKE_MONGOSH'
#!/usr/bin/env bash
set -euo pipefail
state_file=${FAKE_MONGO_STATE:?}
calls_file=${FAKE_MONGO_CALLS:?}
mode=$(<"$state_file")
eval_script="$*"
if [[ "$eval_script" == *'rs.initiate'* ]]; then
  printf 'init\n' >> "$calls_file"
  if [[ "$mode" == uninitialized ]]; then
    printf 'primary\n' > "$state_file"
    exit 0
  fi
  exit 1
fi

printf 'status\n' >> "$calls_file"
case "$mode" in
  temporary)
    status_calls=$(grep -c '^status$' "$calls_file")
    if (( status_calls >= 2 )); then printf 'uninitialized\n' > "$state_file"; fi
    exit 76
    ;;
  uninitialized) exit 94 ;;
  primary) exit 0 ;;
  wrong-set) exit 3 ;;
  timeout) exit 1 ;;
  *) exit 2 ;;
esac
FAKE_MONGOSH
chmod +x "$fake_mongosh"

export PATH="$tmp_dir:$original_path"
export FRESHCART_MONGO_INIT_LIB_ONLY=1
source "$mongo_initializer"
export MONGO_INITDB_ROOT_USERNAME=test MONGO_INITDB_ROOT_PASSWORD=test
export MONGO_REPLICA_SET_POLL_INTERVAL_SECONDS=0.05

printf 'temporary\n' > "$tmp_dir/mongo-state"
: > "$tmp_dir/mongo-calls"
export FAKE_MONGO_STATE="$tmp_dir/mongo-state" FAKE_MONGO_CALLS="$tmp_dir/mongo-calls"
MONGO_REPLICA_SET_TIMEOUT_SECONDS=30 initialize_mongo_replica_set
grep -Fx 'primary' "$tmp_dir/mongo-state" >/dev/null
grep -Fx 'init' "$tmp_dir/mongo-calls" >/dev/null
echo "temporary standalone -> uninitialized -> initiated primary passed"

printf 'primary\n' > "$tmp_dir/mongo-state"
: > "$tmp_dir/mongo-calls"
MONGO_REPLICA_SET_TIMEOUT_SECONDS=30 initialize_mongo_replica_set
if grep -Fx 'init' "$tmp_dir/mongo-calls" >/dev/null; then
  echo "existing primary was reinitialized" >&2
  exit 1
fi
echo "existing primary without reinit passed"

printf 'wrong-set\n' > "$tmp_dir/mongo-state"
: > "$tmp_dir/mongo-calls"
if MONGO_REPLICA_SET_TIMEOUT_SECONDS=30 initialize_mongo_replica_set >/dev/null 2>&1; then
  echo "wrong replica set was accepted" >&2
  exit 1
fi
if grep -Fx 'init' "$tmp_dir/mongo-calls" >/dev/null; then
  echo "wrong replica set was reinitialized" >&2
  exit 1
fi
echo "wrong set failed closed without reinit passed"

printf 'timeout\n' > "$tmp_dir/mongo-state"
: > "$tmp_dir/mongo-calls"
if MONGO_REPLICA_SET_TIMEOUT_SECONDS=1 initialize_mongo_replica_set >/dev/null 2>&1; then
  echo "replica initializer ignored deadline" >&2
  exit 1
fi
echo "deadline failure passed"

echo "freshcart-e2e-stack: production initializer regression checks passed"
