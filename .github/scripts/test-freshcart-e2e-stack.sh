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
  elif [[ "$1 $2" == 'rm -f' ]]; then printf 'container:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/containers.before" "$tmp_dir/containers.current"
  elif [[ "$1 $2" == 'volume rm' ]]; then printf 'volume:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/volumes.before" "$tmp_dir/volumes.current"
  elif [[ "$1 $2" == 'network rm' ]]; then printf 'network:%s\n' "${*:3}" >> "$tmp_dir/removals"; cp "$state/networks.before" "$tmp_dir/networks.current"
  else return 1
  fi
}
cleanup "$state"
wait "$owned_group" 2>/dev/null || true
if kill -0 -- "-$owned_group" 2>/dev/null; then echo "cleanup left its process group running" >&2; exit 1; fi
grep -Fx 'container:new-container' "$tmp_dir/removals"
grep -Fx 'volume:new-volume' "$tmp_dir/removals"
grep -Fx 'network:new-network' "$tmp_dir/removals"
if grep -F 'existing-' "$tmp_dir/removals"; then echo "cleanup removed a pre-existing resource" >&2; exit 1; fi

echo "freshcart-e2e-stack: 5 checks passed"
