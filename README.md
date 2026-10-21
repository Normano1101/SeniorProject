# Adaptive Tower Defense Game

## Senior Project — Fall 2026

A 2D tower defense game developed in Unity and C# for our CSCI 4390 senior project.

## Team

* Daniel Aguilar
* Norman Madrid
* Mark Munoz
* Guillermo Cortez

## Project Overview

The goal of this project is to develop a 2D tower defense game with an adaptive difficulty system. Instead of using completely fixed difficulty levels, the game will adjust future enemy waves based on how the player performs.

The system may consider factors such as:

* Player health
* Enemies defeated
* Enemies reaching the objective
* Time taken to complete a wave
* Overall player performance

The goal is to keep the game challenging while avoiding difficulty that is consistently too easy or too difficult for the player.

## Technologies

* Unity
* C#
* Git
* GitHub

## Development

The team will use GitHub for version control and collaboration. Development will be organized using feature branches, with completed work reviewed and merged into the `main` branch.

## Project Status

An early playable prototype is available in the `Map1` scene.

## Current Playable Prototype

The `Map1` scene includes a replayable tower-defense loop:

* Start with $200, enough to choose tower1 or tower2 but not tower3.
* Defeating enemies awards currency; enemies that reach the base do not.
* Buy tower1 ($100), tower2 ($160), and tower3 ($220) from the right-side shop. Its preview icons show each tower's appearance without covering the map.
* Click an open tile to place the selected tower. Path tiles and occupied spots are blocked; press Esc or right-click to cancel.
* Runners arrive from wave 2 and tougher Heavy enemies from wave 3. Their speed, health, damage, and rewards differ.
* Waves grow over time. Start the next wave early for a small currency bonus, or use the intermission to build.
* The run ends when base health reaches zero. Restarting begins with the starting currency again.

Tower, enemy, health, wave, and currency data are included in the existing save/load flow.
