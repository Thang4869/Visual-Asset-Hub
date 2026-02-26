# ============================================================
# VAH Frontend — Multi-stage Dockerfile
# Build: Node → Serve: Nginx
# ============================================================

# --- Build stage ---
FROM node:22-alpine AS build
WORKDIR /app

# Accept API URL as build arg (used by Vite at build time)
ARG VITE_API_URL=http://localhost:5027/api
ENV VITE_API_URL=${VITE_API_URL}

# Copy package files and install dependencies
COPY package.json package-lock.json* ./
RUN npm ci

# Copy source and build
COPY . ./
RUN npm run build

# --- Serve stage ---
FROM nginx:alpine AS runtime

# Copy custom nginx config for SPA routing
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Copy built files
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
