#!/bin/sh
set -e

# 1. Extract nameservers from /etc/resolv.conf, or fallback to standard Docker/DNS resolvers
RESOLVERS=$(awk 'BEGIN{ORS=" "} $1=="nameserver" {print $2}' /etc/resolv.conf)
if [ -z "$RESOLVERS" ]; then
    RESOLVERS="127.0.0.11 1.1.1.1 8.8.8.8"
fi

# 2. Determine target backend URL from environment or default to internal service
TARGET_URL="${BACKEND_URL:-http://backend:8080}"
case "$TARGET_URL" in
    http://*|https://*) ;;
    *) TARGET_URL="http://$TARGET_URL" ;;
esac

echo "Configuring Nginx with DNS resolvers: $RESOLVERS"
echo "Configuring Nginx with backend target: $TARGET_URL"

sed -i "s/__RESOLVER__/$RESOLVERS/g" /etc/nginx/nginx.conf
sed -i "s|__BACKEND_URL__|$TARGET_URL|g" /etc/nginx/nginx.conf

exec "$@"
