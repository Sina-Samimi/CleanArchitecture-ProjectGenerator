#!/bin/bash

echo "========================================"
echo "  Project Generator - Console Version"
echo "========================================"
echo ""
echo "Note: Windows Forms requires Windows OS"
echo "Running Console version instead..."
echo ""
echo "Building..."

cd ProjectGenerator
dotnet build

if [ $? -ne 0 ]; then
    echo ""
    echo "Build FAILED!"
    read -p "Press Enter to exit..."
    exit 1
fi

echo ""
echo "Build successful! Starting application..."
echo ""
dotnet run

read -p "Press Enter to exit..."
