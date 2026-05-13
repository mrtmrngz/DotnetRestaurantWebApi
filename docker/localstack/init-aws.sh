#!/bin/bash
set -e

echo "S3 bucket oluşturuluyor..."

# Bucket varsa geç, yoksa oluştur
awslocal s3api head-bucket --bucket images-bucket 2>/dev/null \
  || awslocal s3 mb s3://images-bucket --region eu-west-1

# CORS ayarla
awslocal s3api put-bucket-cors \
  --bucket images-bucket \
  --cors-configuration '{
    "CORSRules": [
      {
        "AllowedOrigins": ["*"],
        "AllowedMethods": ["GET", "PUT", "POST", "DELETE"],
        "AllowedHeaders": ["*"],
        "MaxAgeSeconds": 3000
      }
    ]
  }'

# Public read policy
awslocal s3api put-bucket-policy \
  --bucket images-bucket \
  --policy '{
    "Version": "2012-10-17",
    "Statement": [
      {
        "Sid": "PublicRead",
        "Effect": "Allow",
        "Principal": "*",
        "Action": ["s3:GetObject"],
        "Resource": "arn:aws:s3:::images-bucket/*"
      }
    ]
  }'

echo "S3 hazır: images-bucket"