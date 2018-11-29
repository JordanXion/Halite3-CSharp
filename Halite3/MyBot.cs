using Halite3.hlt;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Halite3
{
    public class MyBot
    {
        public static void Main(string[] args)
        {
            int rngSeed;
            if (args.Length > 1)
            {
                rngSeed = int.Parse(args[1]);
            }
            else
            {
                rngSeed = System.DateTime.Now.Millisecond;
            }
            Random rng = new Random(rngSeed);

            Game game = new Game();
            // At this point "game" variable is populated with initial map data.
            // This is a good place to do computationally expensive start-up pre-processing.
            // As soon as you call "ready" function below, the 2 second per turn timer will start.
            game.Ready("JordanXion");

            Log.LogMessage("Successfully created bot! My Player ID is " + game.myId + ". Bot rng seed is " + rngSeed + ".");

            while (true)
            {
                game.UpdateFrame();
                Player me = game.me;
                GameMap gameMap = game.gameMap;

                List<Command> commandQueue = new List<Command>();
                List<Position> selectedChoices = new List<Position>();


                foreach (Ship ship in me.ships.Values)
                {
                    List<Position> moveOptions = ship.position.GetSurroundingCardinals();
                    moveOptions.Add(ship.position);

                    IDictionary<Position, int> haliteOptions = new Dictionary<Position, int>();
                    foreach (Position pos in moveOptions)
                        haliteOptions.Add(pos, gameMap.At(pos).halite); 


                    //If halite at current time is less than 10% of the ships max halite, or if the ship is full of halite
                    if (gameMap.At(ship).halite < Constants.MAX_HALITE / 10 || ship.IsFull())
                    {
                        Direction targetDirection = chooseDirection(ship, haliteOptions, game, selectedChoices);
                        Position target = gameMap.At(ship).position.DirectionalOffset(targetDirection);
                        if (!gameMap.At(target).IsOccupied())
                        {
                            selectedChoices.Add(target);
                            commandQueue.Add(ship.Move(targetDirection));

                        }
                        else
                        {
                            selectedChoices.Add(ship.position);
                            commandQueue.Add(ship.StayStill());
                        }
                    }
                    else if (gameMap.At(ship).halite > Constants.MAX_HALITE - Constants.MAX_HALITE * 0.2f)
                    {
                        commandQueue.Add(ship.Move(gameMap.NaiveNavigate(ship, me.shipyard.position)));
                    }
                    else
                    {
                        
                        commandQueue.Add(ship.Move(Direction.STILL));
                    }
                }

                if ( game.turnNumber <= 200 && me.halite >= Constants.SHIP_COST && !gameMap.At(me.shipyard).IsOccupied() )
                {
                    commandQueue.Add(me.shipyard.Spawn());
                }

                game.EndTurn(commandQueue);
            }
        }

        static Direction chooseDirection(Ship ship, IDictionary<Position, int> haliteOptions, Game game, List<Position> preselected)
        {
            bool found = false;
            Direction targetDirection = Direction.WEST;
            IDictionary<Position, int> temp = haliteOptions;
            foreach (Position pos in preselected)
            {
                Position x = new Position(pos.x - ship.position.x, pos.y - ship.position.y);
                if (temp.ContainsKey(x))
                {
                    temp.Remove(x);
                }
            }
            while (!found)
            {
                Position highestHalite = temp.Aggregate((l, r) => l.Value > r.Value ? l : r).Key;
                if (!game.gameMap.At(highestHalite).IsOccupied())
                {

                    Position diff = new Position(highestHalite.x - ship.position.x, highestHalite.y - ship.position.y);
                    if (diff.x == 0)
                    {
                        if (diff.y == 0)
                            targetDirection = Direction.STILL;
                        else if (diff.y == 1)
                            targetDirection = Direction.NORTH;
                        else if (diff.y == -1)
                            targetDirection = Direction.SOUTH;
                    }
                    else if (diff.x == 1)
                    {
                        if (diff.y == 0)
                            targetDirection = Direction.EAST;
                    }
                    else if (diff.x == -1)
                    {
                        if (diff.y == 0)
                            targetDirection = Direction.WEST;
                    }
                    found = true;
                }
                else
                {
                    temp.Remove(highestHalite);
                }
            }
            Log.LogMessage(string.Format("Ship {0} is going {1}", ship.id, targetDirection));
            return targetDirection;
        }    
    }
}
