using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using System.Diagnostics;
using Unity.Entities;

namespace SimpleDrama
{
    public class DramaFinder : MonoBehaviour
    {
        // Stages
        public List<Stage> Stages { get; set; } = new List<Stage>();

        // Plays
        public List<Play> Plays { get; set; } = new List<Play>();

        private void Start()
        {
            #region Create test data

            var numberOfStages = 1000;
            var numberOfPlays = 400;
            var ssPerStage = 24;
            var maxPsPerPlay = 5;

            for (int i = 0; i < numberOfStages; i++)
            {
                // Create stage
                var someStage = new Stage();
                someStage.stageId = i;
                someStage.needsPlay = true;

                for (int j = 0; j < ssPerStage; j++)
                {
                    // Add a situation to the stage
                    var someSituation = new Situation();

                    someSituation.Parameters = new List<Parameter>()
                {
                    new NumberOfActors(UnityEngine.Random.Range(0, 4)),
                    new RelationshipTemplate(UnityEngine.Random.Range(0, 4), (RelationshipType)UnityEngine.Random.Range(0, 4), UnityEngine.Random.Range(0, 4))
                };

                    someStage.Situations.Add(someSituation);
                }

                Stages.Add(someStage);
            }

            for (int i = 0; i < numberOfPlays; i++)
            {
                // Create play
                var somePlay = new Play();
                somePlay.playId = i;

                var numberOfParameters = UnityEngine.Random.Range(0, maxPsPerPlay);

                for (int j = 0; j < numberOfParameters; j++)
                {
                    if (UnityEngine.Random.Range(0, 1) % 2 == 0)
                    {
                        somePlay.PlayParameters.Add(new PlayParameter<NumberOfActors>(x => x.value >= UnityEngine.Random.Range(0, 4)));
                    }
                    else
                    {
                        somePlay.PlayParameters.Add(new PlayParameter<RelationshipTemplate>(x => x.aPosition == UnityEngine.Random.Range(0, 4) && x.rType == (RelationshipType)UnityEngine.Random.Range(0, 4) && x.bPosition == UnityEngine.Random.Range(0, 4)));
                    }
                }

                Plays.Add(somePlay);
            }

            #endregion

            StartCoroutine(PlaySearch());
        }

        private Stopwatch s = new Stopwatch();

        IEnumerator PlaySearch()
        {
            s.Start();
            var validPlays = new List<int>();

            foreach (var stage in Stages)
            {
                if (!stage.needsPlay) break;

                var time = Time.deltaTime; // Cache

                foreach (var situation in stage.Situations)
                {
                    foreach (var play in Plays)
                    {
                        if (play.PlayIsValid(situation))
                        {
                            validPlays.Add(play.playId);
                        }

                        // Yield if 1/60th of a second has passed.
                        if (s.ElapsedMilliseconds >= 10) { s.Restart(); yield return null; }
                    }
                }

                // Do something with play
                // Should ship with a situation
                if (validPlays.Count == 0) break;

                RunPlay(stage.stageId, validPlays[UnityEngine.Random.Range(0, validPlays.Count)]);
                validPlays.Clear();
            }

            StartCoroutine(PlaySearch());
        }

        private void RunPlay(int stage, int playId)
        {
            UnityEngine.Debug.Log("Found Play");
        }
    }

    public struct Situation
    {
        public int value;
        public List<Parameter> Parameters;
        // Possibly add a list of valid plays
    }

    public class Parameter { }

    public abstract class PlayParameter
    {
        public abstract bool IsApplicable(Parameter parameter);
    }

    public class PlayParameter<T> : PlayParameter where T : Parameter
    {
        public Predicate<T> ValidCheck { get; set; }

        public PlayParameter(Predicate<T> validCheck)
        {
            this.ValidCheck = validCheck;
        }

        public override bool IsApplicable(Parameter parameter)
        {
            if (!(parameter is T typedParameter))
                return false;

            return ValidCheck(typedParameter);
        }
    }

    public class RelationshipTemplate : Parameter
    {
        public int aPosition;
        public RelationshipType rType;
        public int bPosition;

        public RelationshipTemplate(int _aPosition, RelationshipType _rType, int _bPosition)
        {
            aPosition = _aPosition;
            rType = _rType;
            bPosition = _bPosition;
        }
    }

    public class NumberOfActors : Parameter
    {
        public int value;

        public NumberOfActors(int _value)
        {
            value = _value;
        }
    }

    public class Play
    {
        public int playId;
        public List<PlayParameter> PlayParameters { get; set; } = new List<PlayParameter>();

        public bool PlayIsValid(Situation p)
        {
            return PlayParameters.All(parameter => p.Parameters.Any(param => parameter.IsApplicable(param)));
        }
    }

    public class Stage
    {
        public int stageId;
        public List<Situation> Situations { get; set; } = new List<Situation>();
        public bool needsPlay;
    }

    public enum RelationshipType
    {
        DoesntKnow,
        Knows,
        Loves,
        Hates
    }
}
