resource "aws_db_subnet_group" "this" {
  name       = "${var.name_prefix}-dbsubnet"
  subnet_ids = var.subnet_ids
  tags       = merge(var.tags, { Name = "${var.name_prefix}-dbsubnet" })
}

# Security group allowing only within VPC (adjust per app needs)
resource "aws_security_group" "db" {
  name        = "${var.name_prefix}-db-sg"
  description = "DB access"
  vpc_id      = var.vpc_id
  ingress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["10.0.0.0/8"]
    description = "Example wide-open VPC range; tighten with app SGs"
  }
  egress {
    from_port   = 0
    to_port     = 0
    protocol    = "-1"
    cidr_blocks = ["0.0.0.0/0"]
  }
  tags = var.tags
}

# Aurora PostgreSQL (default)
resource "aws_rds_cluster" "aurora" {
  count                   = var.engine == "aurora-postgresql" ? 1 : 0
  engine                  = "aurora-postgresql"
  engine_mode             = "provisioned"
  master_username         = var.master_username
  master_password         = var.master_password
  db_subnet_group_name    = aws_db_subnet_group.this.name
  vpc_security_group_ids  = [aws_security_group.db.id]
  backup_retention_period = 7
  storage_encrypted       = true
  tags                    = var.tags
}

resource "aws_rds_cluster_instance" "aurora_instances" {
  count               = var.engine == "aurora-postgresql" ? 1 : 0
  identifier          = "${var.name_prefix}-aurora-0"
  cluster_identifier  = aws_rds_cluster.aurora[0].id
  instance_class      = var.instance_class
  engine              = aws_rds_cluster.aurora[0].engine
  publicly_accessible = false
  tags                = var.tags
}

# SQL Server example (developer/express)
resource "aws_db_instance" "sqlserver" {
  count                  = var.engine == "sqlserver-ex" ? 1 : 0
  identifier             = "${var.name_prefix}-sql"
  engine                 = "sqlserver-ex"
  instance_class         = var.instance_class
  username               = var.master_username
  password               = var.master_password
  db_subnet_group_name   = aws_db_subnet_group.this.name
  vpc_security_group_ids = [aws_security_group.db.id]
  allocated_storage      = 50
  storage_encrypted      = true
  publicly_accessible    = false
  skip_final_snapshot    = true
  tags                   = var.tags
}

output "db_endpoint" {
  value = coalesce(
    try(aws_rds_cluster.aurora[0].endpoint, null),
    try(aws_db_instance.sqlserver[0].address, null)
  )
}
