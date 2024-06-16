# RigidPlacement
An editor tool for Unity to place things in a scene using Rigidbodies and Physics Simulation, inspired by Sebastian Lague's video on the topic: https://www.youtube.com/watch?v=SnhfcdtGM2E
There are many tools like this one out there on the Asset store and Github repositories out there but this one is MIT licensed and so all code is free to be changed by you or anyone else.

Feel free to make pull requests for this repo if you wish to contribute!

# Requirements
This version was made using Unity 2022.3.32f1 however most of the code difference between now and older editions of Unity are syntactic, not functionality.
I might look into testing this on as old a version of Unity as I can so that I may support older editions too.

# Workflow
<img align="right" width="343" height="433" src="https://github.com/mads-fs/rigid-placement/assets/146040109/831125db-0a51-4fd1-893b-4e38120d8240">
The project consists of a single Editor script that places a new "Rigid Placement" menu item under "Window" and that's all you need for it to work.
The worflow is kept simple:
1. Choose Window->Rigid Placement
2. A smaller window pops up:
    * Max Iterations: The number of Physics Steps before the simulation stops. This is important to set because if an object falls off the world you don't want the simulation to go off indefinitely.
    * Min|Max Force: A range of force that can be added to the simulation randomly to each object.
    * Randomize Angle: Checking this will introduce a randomized angle force to the simulation for each object.
    * Force Angle (Degrees): If you wish to dial in a specific angle rather than randomize the angle.
    * Include Non-Rigidbodies: When working with objects that do not have rigidbodies but you still wish to place with Physics check this box and add them to the list.
    * Add: Will add what objects to Simulate. Pick objects in the scene view. Objects without Rigidbodies won't be added to the simulation window unless "Include Non-Rigidbodies" is checked.
    * Remove: Whatever objects you've chosen in the scene view will be removed from the list of simulated bodies if they are in the list.
    * Clear: Will clear out all objects from the simulated bodies list.
    * Show Simulation: Will toggle on or off whether you wish to see the simulation in real-time or if you just want the result of the simulation.
    * Simulate: Will use Physics simulations to let the objects be effect by physics the number of iterations steps you input earlier or until all bodies rest. Be aware that when you press Simulate the current position and rotation are both noted. So if you press Simulate twice by accident then "Reset Bodies" nor Ctrl+Z cannot bring the bodies back to where they were.
    * Reset Bodies: This will reset all simulated bodies to their previous position before the Simulate button was pressed. You cannot Ctrl-Z your way back to previous positions/rotation, so this is to make up for that.
    * Status: Will print small messages from the tool itself.
    * Overview: The list of objects you currently will affect when you press "Simulate".

Stay tuned for possibly more features and tweaks!

# Demonstration
https://github.com/mads-fs/rigid-placement/assets/146040109/7ac92564-a2c6-4bab-a862-f6150bc1ae2c
