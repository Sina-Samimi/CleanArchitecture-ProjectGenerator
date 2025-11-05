#!/bin/bash

echo "========================================"
echo "  Project Generator - Build and Run"
echo "========================================"
echo ""

show_menu() {
    echo "Please select an option:"
    echo ""
    echo "1. Build All Projects"
    echo "2. Run Console Application"
    echo "3. Clean Solution"
    echo "4. Exit"
    echo ""
}

build_all() {
    echo ""
    echo "Building all projects..."
    dotnet restore ProjectGenerator.sln
    dotnet build ProjectGenerator.sln
    echo ""
    echo "Build completed!"
    read -p "Press Enter to continue..."
}

run_console() {
    echo ""
    echo "Running Console Application..."
    cd ProjectGenerator
    dotnet run
    cd ..
    read -p "Press Enter to continue..."
}

clean_solution() {
    echo ""
    echo "Cleaning solution..."
    dotnet clean ProjectGenerator.sln
    echo ""
    echo "Clean completed!"
    read -p "Press Enter to continue..."
}

while true; do
    clear
    echo "========================================"
    echo "  Project Generator - Build and Run"
    echo "========================================"
    echo ""
    show_menu
    read -p "Enter your choice (1-4): " choice
    
    case $choice in
        1) build_all ;;
        2) run_console ;;
        3) clean_solution ;;
        4) echo ""; echo "Goodbye!"; exit 0 ;;
        *) echo "Invalid option. Please try again."; sleep 2 ;;
    esac
done
