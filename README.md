# asteroid-collision
Purpose of this game is to show collision detection system optimized for large numbers of 2d circular objects I created.

In test scene there are 25,600 asteroids. All asteroids are simulated at all times and collide with eachother, player and player's bullets. Asteroids have random velocity and on collision are destroyed and respawned after 1s. Player gets points for destroying asteroids with his bullets and looses when asteroid hits him. After being hit by asteroid player can restart game with "restart" button which quickly restarts game.

Whole system is optimized for large number of asteroids:
* only one GameObject which acts as manager for all asteroids
* only asteroids close to player are drawn
* simulation space divided in cells to reduce number of collision tests
* testing for collision between circles means only comparing distances (squared) between objects
* game can be restarted without any lag as it's state at the start is cached

On laptop with Intel Core i7-6500U and 8GB RAM easliy 60FPS in test scene.
