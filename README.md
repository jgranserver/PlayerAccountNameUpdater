# Player Account Name Updater

A TShock plugin that automatically detects and helps users update their account names when they differ from their player names.

## Features

- Automatically detects when a player's account name differs from their in-game name
- Provides a secure way to update account names using password verification
- Sends periodic reminders to players with pending name changes

## Installation

1. Download the latest release from the releases page
2. Place the `PlayerAccountNameUpdater.dll` file in your server's `ServerPlugins` folder
3. Restart your TShock server

## Usage

When a player logs in with a different name than their account name:
1. They will receive a notification about the name mismatch
2. They can use `/confirmname <password>` to update their account name
3. The plugin will verify their password and update their account name

## Commands

- `/confirmname <password>` - Updates your account name to match your current player name

## Permissions

- `updateaccountname.confirm` - Allows players to use the `/confirmname` command

## Configuration

No configuration needed. The plugin works out of the box.

## Building from Source

1. Clone the repository
2. Open the solution in Visual Studio or VS Code
3. Restore NuGet packages
4. Build the solution

## Author

Created by jgranserver