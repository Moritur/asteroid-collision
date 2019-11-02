# asteroid-collision
Purpose of this game is to show collision detection system optimized for large numbers of 2d circular objects I created.

In test scene there are 25,600 asteroids. All asteroids are simulated at all times and collide with eachother, player and player's bullets. Asteroids have random velocity and on collision are destroyed and respawned after 1s. Player gets points for destroying asteroids with their bullets and looses when asteroid hits them. After being hit by asteroid player can restart game with "restart" button which quickly restarts game.

Whole system is optimized for large number of asteroids:
* most of physics code is run in parallel
* there is only one GameObject which acts as manager for all asteroids
* only asteroids close to player are drawn
* simulation space is divided in cells to reduce number of collision tests
* testing for collision between circles means only comparing squared distances between objects
* game can be restarted without any lag as it's state at the start is cached

On laptop with Intel Core i5-8300H and 8GB 1200 MHz RAM ~160FPS in test scene.
