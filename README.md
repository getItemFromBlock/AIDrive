# AI Drive

The goal of this project was to experiment with artificial intelligence, more precisely neural networks, while staying in the field of video games.
I decided to make an AI that learn to drive a car in a 3D karting environment, but with added physics to keep the project a little different from the usual 2D-driving project often made in this context.

## Goals

My original goal was to make an AI that can drive in a circuit with multiple cars in an autonomous way. I planned to add “objects” which would behave like items in the Mario Kart games (bananas, bombs, shells, etc.) but due to a lack of time I had to cut the objects.

In the end, I was still happy that I managed to train the AI enough for it to be able to drive with multiple cars in a circuit, and the finished project includes a map where you can drive a car with 3 AI cars.

## Difficulties

I encountered a lot of problems with this project, in particular with the training. The training circuit has right and left turns, slopes, and a zigzagged part, which was the most difficult for the AI.

Given that the project was heavily reliant on physics, I made sure to give the AI all sort of information related to the car, such as the velocity, pitch, roll, whether or not each wheel is touching the ground; and a value to tell whether or not the car is going the wrong way.

The AI are also given a set of 12 ray casts which can see the borders of the circuit, and another set of 8 ray cast which can see both the borders and other cars.

For the genetic algorithm, I decided to only evaluate the AIs, because there was no good option for me to use backpropagation. The training is made in 3 steps. The AI are let free on the circuit, then they are sorted by score, and finally they are replicated and mutated based on their score.

The score calculation is based on a set of checkpoints placed on the circuit, that gives point when the car moves towards the next checkpoint and adds a bonus if a car reach it. The system can also go backward and remove points if the car goes in the wrong direction. If the car flips over, goes outside of the circuit, or touch a wall, points are also removed.

The most difficult part was to train the AI to learn how to drive in the circuit, because a lot of my initial code for this project was not really helping the AI learn.

Sorting the AI based on their score didn’t pose a problem, but the next part was not so easy. I had to find an algorithm to make the best cars replicates more and punish the other ones without completely removing them. After some experiments I decided to go with an algorithm that considers the amount of point the car has based on the average score of all cars. Some randomness is involved, so that low scoring cars also have a chance of being present, whereas the top scoring cars will always be present. This also makes it so that if the score difference between the first car and the others is big, there will be much more of the first car in the new generation. To help with the low score difference that would happen at the end, I also made the first car receive a bonus based on the generation number.

The next hard part was the mutation. I first tried to mutate all the neural network, but it just made things worse as the mutation were almost always making bad changes to the network. What really helped me here is when I changed the algorithm to only mutate 3 connection weight or neuron bias. This made it much easier to have good changes without destroying all the network.

Even with this solved, there was still problems with my method. The AI was managing to somewhat navigates the first half of the circuit, but it was driving in a strange way, by bonking into walls to help it flips over and negotiate some turns. The AI was also unable to pass the zigzagged part, no mater how long I trained them. I tried a lot of things to overcome this problem.

## Experiments

My first thought was to change the punishment for touching a wall. I first put a high punishment, but it just made the AI stay in place. I then added another punishment if the AI don’t move, but that was not working either. I tried lowering, then completely removing the wall punishment but the AI was still not learning correctly to avoid the walls.
Solution
Given the strange way the AI was driving, I realized that the AI was using the value I originally gave it to know whether it is driving forward to just drive, and because of that it was ignoring the ray cast inputs. My first idea was to change from an angle to a 1 or -1 value, which would prevent the AI from using it like it did. After that, the AI was still refusing to learn to use the ray casts. I tried changing the ray casts input, to gives a linear depth value (the max depth can be changed in the configs) and transform it to a value between 0 and 1, where 1 is close to the car and 0 is the max depth. This seemed to help, but as an ultimate change I decided to completely remove all the other inputs and only leave the ray casts. This time the AI learned to navigate the circuit and could make a full turn most of the time after 300 generations.

I had finally made progress, but it was far from finished. I still had to come with a way to add the missing inputs and train the AI to evolve with other cars in the circuit (to make things easier I first trained the AI with one car per circuit). Resizing the neural network was not easy, but I figured out that adding the missing inputs with very small weights and bias was giving good results. After that, I trained the Ai for around 1000 generations, and they started to become quite good at driving, which made me think that they correctly learned to use the other inputs.

The next big step was to have multiple cars on a circuit, but this was not very hard to do since most of the code was already done. I also did not forget to include the additional ray casts. After a lot of training, I decided to stop the program at 3000 generations.

## Results

The AI had become really good, and even if it can still get stuck in some cases, or rarely go backward, the AI also showed behaviors that I was not expecting. I observed that when the AI hit a wall and lose control of its car, the AI can maneuver and turn around to continue driving. The AI also know to use the brake in order to slow down before tight turns when it has a lot of speed.

The results of this can be seen within the project, in the scene GameScene0 where the player can drive against 3 AI car.
