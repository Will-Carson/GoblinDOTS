using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

namespace Tessera
{
    [CustomEditor(typeof(AnimatedGenerator))]
    class AnimatedGeneratorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            var ag = (AnimatedGenerator)target;
            if (ag.IsRunning)
            {
                if (GUILayout.Button("Pause"))
                {
                    ag.PauseGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update -= ag.Step;
                    }
                }
                if (GUILayout.Button("Stop"))
                {
                    ag.StopGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update -= ag.Step;
                    }
                }
            }
            else
            {
                if (ag.IsStarted)
                {
                    if (GUILayout.Button("Resume"))
                    {
                        ag.ResumeGeneration();

                        if (!Application.isPlaying)
                        {
                            EditorApplication.update += ag.Step;
                        }
                    }
                }
                if (GUILayout.Button("Start"))
                {
                    ag.StartGeneration();

                    if (!Application.isPlaying)
                    {
                        EditorApplication.update += ag.Step;
                    }
                }
            }
        }
    }
}
