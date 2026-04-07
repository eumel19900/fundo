# Copilot Instructions

## Project Guidelines
- Use English for user-facing UI messages and hints in this project.

## Code Structure
- When extracting command line handling in this project, move the full launch decision into the service class instead of keeping argument-specific checks in App.xaml.cs.

## Regex in SQL queries
 - In the SearchIndexContext, a custom SQL function for regex handling is registered when creating the DbContext. The SQL function is called "REGEXP."