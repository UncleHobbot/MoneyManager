#!/bin/sh
set -e

DB_PATH="/app/data/MoneyManager.db"
TEMPLATE_PATH="/app/template/MoneyManagerEmpty.db"

# Copy empty template database if no database exists yet
if [ ! -f "$DB_PATH" ] && [ -f "$TEMPLATE_PATH" ]; then
    echo "No database found at $DB_PATH — initializing from template..."
    cp "$TEMPLATE_PATH" "$DB_PATH"
    echo "Database initialized."
fi

exec dotnet MoneyManager.Api.dll "$@"
