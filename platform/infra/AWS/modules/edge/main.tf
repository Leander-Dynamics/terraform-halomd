# Minimal placeholder for CloudFront + WAF + Route53
# Expect an ALB or external origin to be passed in later (not created here)

resource "aws_wafv2_web_acl" "this" {
  name  = "${var.name_prefix}-waf"
  scope = "CLOUDFRONT"

  default_action {
    allow {}
  }

  visibility_config {
    cloudwatch_metrics_enabled = true
    metric_name                = "${var.name_prefix}-waf"
    sampled_requests_enabled   = true
  }
  tags = var.tags
}

resource "aws_cloudfront_distribution" "this" {
  enabled = true

  origin {
    domain_name = "example.com" # TODO: replace with ALB/NLB or S3 origin
    origin_id   = "origin-1"
    custom_origin_config {
      http_port              = 80
      https_port             = 443
      origin_protocol_policy = "https-only"
      origin_ssl_protocols   = ["TLSv1.2"]
    }
  }

  default_cache_behavior {
    allowed_methods        = ["GET", "HEAD", "OPTIONS"]
    cached_methods         = ["GET", "HEAD"]
    target_origin_id       = "origin-1"
    viewer_protocol_policy = "redirect-to-https"
    forwarded_values {
      query_string = true
      cookies { forward = "none" }
    }
  }

  restrictions {
    geo_restriction { restriction_type = "none" }
  }
  viewer_certificate { cloudfront_default_certificate = true }

  web_acl_id = aws_wafv2_web_acl.this.arn
  tags       = var.tags
}

resource "aws_route53_record" "root" {
  zone_id = var.hosted_zone_id
  name    = var.domain_name
  type    = "A"

  alias {
    name                   = aws_cloudfront_distribution.this.domain_name
    zone_id                = aws_cloudfront_distribution.this.hosted_zone_id
    evaluate_target_health = false
  }
}

output "cloudfront_domain" { value = aws_cloudfront_distribution.this.domain_name }
