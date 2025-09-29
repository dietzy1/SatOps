#!/bin/bash

# EF Core Migration Helper Script
# Usage: ./add-migration.sh MigrationName

if [ -z "$1" ]; then
    echo "Error: Migration name is required"
    echo "Usage: ./add-migration.sh MigrationName"
    echo "Example: ./add-migration.sh AddUserTable"
    exit 1
fi

MIGRATION_NAME=$1

echo "Creating migration: $MIGRATION_NAME"
echo "Output directory: Data/Migrations"

# Create the migration with the specified output directory
dotnet ef migrations add "$MIGRATION_NAME" --output-dir Data/Migrations

if [ $? -eq 0 ]; then
    echo "✅ Migration '$MIGRATION_NAME' created successfully in Data/Migrations/"
    echo ""
    echo "To apply this migration to the database, run:"
    echo "   dotnet ef database update"
else
    echo "❌ Failed to create migration"
    exit 1
fi
