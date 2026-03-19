#!/bin/bash

# DockerHub Info
DOCKER_USER="rosie91"
DOCKER_REPO="stemify"

# Service Mapping
declare -A SERVICES
SERVICES=(
  ["classroom"]="src/Services/ClassroomService/Classroom.API classroom-service"
  ["resource"]="src/Services/Resource/Resource.API resource-service"
  ["gateway"]="src/ApiGateways/ApiGateway gateway-service"
  ["ai-service"]="src/Services/AIService ai-service"
  ["identity-api"]="src/Services/Identity/Identity.API identity-api-service"
  ["identity-web"]="src/Services/Identity/Identity.Web identity-service"
  ["notification"]="src/Services/Notification/Notification.API notification-service"
  ["product"]="src/Services/ProductService/Product.API product-service"
  ["order"]="src/Services/OrderService/Order.API order-service"
  ["cart"]="src/Services/CartService/Cart.API cart-service"
  ["payment"]="src/Services/PaymentService/Payment.API payment_service"
  ["emulator"]="src/Services/EmulatorService/Emulator.API emulator-service"
)

# Function Build & Push
build_and_push() {
  SERVICE_PATH=${SERVICES[$1]%% *}
  TAG=${SERVICES[$1]#* }

  if [ -z "$SERVICE_PATH" ] || [ -z "$TAG" ]; then
    echo "Service '$1' không hợp lệ!"
    exit 1
  fi

  IMAGE_NAME="$DOCKER_USER/$DOCKER_REPO:$TAG"

  echo "=============================="
  echo "Building: $1"
  echo "Path: $SERVICE_PATH"
  echo "Image: $IMAGE_NAME"
  echo "=============================="

  docker build -f $SERVICE_PATH/Dockerfile -t $IMAGE_NAME .
  if [ $? -eq 0 ]; then
    echo "Build success"
    docker push $IMAGE_NAME
    echo "Pushed: $IMAGE_NAME"
  else
    echo "Build failed for $1"
  fi
}

# CLI Options
if [ "$1" == "all" ]; then
  for svc in "${!SERVICES[@]}"; do
    build_and_push $svc
  done
elif [ -n "$1" ]; then
  build_and_push $1
else
  echo "Usage:"
  echo "./build_and_push.sh all         (build & push tất cả)"
  echo "./build_and_push.sh classroom   (build 1 service - classroom)"
  echo "./build_and_push.sh ai-service  (build 1 service - AI Service)"
  echo "./build_and_push.sh emulator    (build emulator service)"
  echo ""
  echo "Available services:"
  for k in "${!SERVICES[@]}"; do echo " - $k"; done
fi
