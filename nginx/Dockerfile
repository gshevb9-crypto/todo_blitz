FROM nginx:alpine

COPY nginx.conf /etc/nginx/nginx.conf
COPY entrypoint.sh /docker-entrypoint.d/35-inject-resolver.sh
RUN chmod +x /docker-entrypoint.d/35-inject-resolver.sh

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
