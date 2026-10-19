#!/bin/sh
set -e

# Extract nameservers from /etc/resolv.conf, or fallback to standard Docker/DNS resolvers
RESOLVERS=$(awk 'BEGIN{ORS=" "} $1=="nameserver" {print $2}' /etc/resolv.conf)
if [ -z "$RESOLVERS" ]; then
    RESOLVERS="127.0.0.11 1.1.1.1 8.8.8.8"
fi

echo "Configuring Nginx with DNS resolvers: $RESOLVERS"
sed -i "s/__RESOLVER__/$RESOLVERS/g" /etc/nginx/nginx.conf

exec "$@"
