# RigidPlacement
An editor tool for Unity to place things in a scene using Rigidbodies and Physics Simulation, inspired by Sebastian Lague's video on the topic: https://www.youtube.com/watch?v=SnhfcdtGM2E
There are many tools like this one out there on the Asset store and Github repositories out there but this one is MIT licensed and so all code is free to be changed by you or anyone else.

Feel free to make pull requests for this repo if you wish to contribute!

# Workflow
The project consists of a single Editor script that places a new "Rigid Placement" menu point under "Window" and that's all you need for it to work.
The worflow is kept simple:
1. Choose Window->Rigid Placement
2. A smaller window pops up with a field, and a couple of buttons:
    * Max Iterations: The number of Physics Steps before the simulation stops. This is important to set because if an object falls off the world you don't want the simulation to go off indefinitely.
    * Add: Will add what objects to Simulate. Pick objects in the scene view with a Rigidbody on. Objects without Rigidbodies won't be added to the simulation window.
    * Remove: Whatever objects you've chosen in the scene view will be removed from the list of simulated bodies if they are in the list.
    * Clear: Will clear out all objects from the simulated bodies list.
    * Simulate: Will use Physics simulations to let the objects be effect by physics the number of iterations steps you input earlier or until all bodies rest.
    * Reset Bodies: This will reset all simulated bodies to their previous position before the Simulate button was pressed. You cannot Ctrl-Z your way back to previous positions, so this is to make up for that.
    * Status: Will just print small messages from the tool itself.

More features will be included.
