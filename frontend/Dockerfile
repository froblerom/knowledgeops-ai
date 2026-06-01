# KnowledgeOps-AI Frontend — Multi-stage Dockerfile
# Build context: frontend/ directory (docker build -f frontend/Dockerfile ./frontend)
#
# Stage 1: Build — install dependencies and build the Angular application
# Stage 2: Runtime — serve static files from nginx:alpine
#
# Angular project name: frontend (from angular.json "projects": { "frontend": {...} })
# Output path: dist/frontend/browser (default for @angular/build:application in Angular 17+)
#
# No secrets or API keys are baked into this image.
# The frontend connects to the API via environment-configured base URLs at runtime.

FROM node:24-alpine AS build
WORKDIR /app

# Copy package manifests first to exploit Docker layer caching
COPY package.json package-lock.json ./
RUN npm ci

# Copy the rest of the frontend source and build for production
COPY . .
RUN npm run build

FROM nginx:alpine AS runtime

# Copy built output from the Angular @angular/build:application builder.
# Default output path for project named "frontend": dist/frontend/browser/
COPY --from=build /app/dist/frontend/browser /usr/share/nginx/html

EXPOSE 80
