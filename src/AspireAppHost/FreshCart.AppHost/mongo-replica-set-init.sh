#!/usr/bin/env bash
set -euo pipefail

readonly MONGO_REPLICA_SET_NAME="rs0"
readonly MONGO_REPLICA_SET_MEMBER="127.0.0.1:27017"
readonly MONGO_REPLICA_SET_KEYFILE="/data/configdb/replica-set.key"

mongo_eval() {
  mongosh --quiet \
    -u "$MONGO_INITDB_ROOT_USERNAME" \
    -p "$MONGO_INITDB_ROOT_PASSWORD" \
    --authenticationDatabase admin \
    --eval "$1"
}

initialize_mongo_replica_set() {
  local timeout_seconds="${MONGO_REPLICA_SET_TIMEOUT_SECONDS:-600}"
  local poll_interval="${MONGO_REPLICA_SET_POLL_INTERVAL_SECONDS:-1}"
  local deadline=$((SECONDS + timeout_seconds))
  local status_code init_code

  while ((SECONDS < deadline)); do
    if mongo_eval 'try { const status = rs.status(); if (status.set && status.set !== "rs0") quit(3); quit(status.members?.some(member => member.stateStr === "PRIMARY" && member.health === 1) ? 0 : 1); } catch (error) { quit(error.code === 94 || error.code === 76 ? error.code : 1); }' >/dev/null 2>&1; then
      return 0
    else
      status_code=$?
    fi

    case "$status_code" in
      94)
        if mongo_eval 'try { const status = rs.status(); if (status.set && status.set !== "rs0") quit(3); quit(1); } catch (error) { if (error.code === 94) { const result = rs.initiate({_id: "rs0", members: [{_id: 0, host: "127.0.0.1:27017"}]}); quit(result.ok === 1 ? 0 : 1); } quit(error.code === 76 ? 76 : 1); }' >/dev/null 2>&1; then
          :
        else
          init_code=$?
          case "$init_code" in
            0|76) ;;
            3) echo "Mongo replica-set has an unexpected set identity" >&2; return 1 ;;
            *) ;;
          esac
        fi
        ;;
      76|1)
        ;;
      3)
        echo "Mongo replica-set has an unexpected set identity" >&2
        return 1
        ;;
      *)
        ;;
    esac
    sleep "$poll_interval"
  done

  echo "Mongo replica-set primary was not ready before ${timeout_seconds}s" >&2
  return 1
}

prepare_mongo_keyfile() {
  if [[ ! -f "$MONGO_REPLICA_SET_KEYFILE" ]]; then
    openssl rand -base64 756 > "$MONGO_REPLICA_SET_KEYFILE"
    chmod 400 "$MONGO_REPLICA_SET_KEYFILE"
    chown mongodb:mongodb "$MONGO_REPLICA_SET_KEYFILE"
  fi
}

if [[ "${FRESHCART_MONGO_INIT_LIB_ONLY:-}" != 1 ]]; then
  prepare_mongo_keyfile
  (initialize_mongo_replica_set) &
  exec docker-entrypoint.sh mongod \
    --replSet "$MONGO_REPLICA_SET_NAME" \
    --keyFile "$MONGO_REPLICA_SET_KEYFILE" \
    --bind_ip_all
fi
