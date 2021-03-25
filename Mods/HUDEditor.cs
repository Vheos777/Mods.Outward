﻿using HarmonyLib;
using UnityEngine;
using BepInEx.Configuration;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace ModPack
{
    public class HUDEditor : AMod
    {
        // Setting
        static private ModSetting<bool> _startEditMode;
        override protected void Initialize()
        {
            _startEditMode = CreateSetting(nameof(_startEditMode), false);
            _startEditMode.AddEvent(() =>
            {
                if (_startEditMode)
                {
                    _startEditMode.SetSilently(false);
                    Tools.IsConfigOpen = false;
                    StartEditMode();
                };
            });

            AddEventOnConfigOpened(TryStopEditMode);

            _attachedTransformsByOffset = new Dictionary<Transform, Vector3>();
        }
        override protected void SetFormatting()
        {
            _startEditMode.Format("Start Edit Mode");
        }


        private void StartEditMode()
        {

        }
        private void TryStopEditMode()
        {
            if(_isInEditMode)
        }

        public void OnUpdate()
        {
            if (Input.GetKey(KeyCode.LeftAlt))
            {
                if (Input.GetKeyDown(KeyCode.Keypad0))
                {
                    foreach (var localPlayer in GameInput.LocalPlayers)
                    {
                        Transform hudHolder = localPlayer.UI.transform.Find("Canvas/GameplayPanels/HUD");
                        foreach (var canvasGroup in hudHolder.GetAllComponentsInHierarchy<CanvasGroup>())
                            canvasGroup.blocksRaycasts = true;
                    }
                }

                if (Input.GetKeyDown(KeyCode.Keypad1))
                {
                    _isInEditMode = !_isInEditMode;

                    GameInput.ForceCursorNavigation = _isInEditMode;
                    if (_isInEditMode)
                        foreach (var localPlayer in GameInput.LocalPlayers)
                        {
                            Transform hudHolder = localPlayer.UI.transform.Find("Canvas/GameplayPanels/HUD");
                            string[] forceRaycastTargetPanels = new[]
                            {
                            "StatusEffect - Panel",
                            "MainCharacterBars/Mana",
                            };

                            foreach (var panelName in forceRaycastTargetPanels)
                                foreach (var image in hudHolder.Find(panelName).GetAllComponentsInHierarchy<Image>())
                                    image.raycastTarget = true;
                        }
                    else
                        _attachedTransformsByOffset.Clear();
                }
            }

            if (_isInEditMode)
            {
                if (Input.GetKeyDown(KeyCode.Mouse0))
                {
                    foreach (var localPlayer in GameInput.LocalPlayers)
                    {
                        Tools.Log($"{localPlayer.Character.Name}:");
                        Transform hudHolder = localPlayer.UI.transform.Find("Canvas/GameplayPanels/HUD");
                        foreach (var hit in hudHolder.GetOrAddComponent<GraphicRaycaster>().GetMouseHits())
                        {
                            Transform transform = hit.gameObject.transform;
                            Vector3 offset = Input.mousePosition.OffsetTo(transform.position);
                            _attachedTransformsByOffset.Add(transform, offset);
                            Tools.Log($" - {hit.gameObject.name}");
                        }
                        Tools.Log($"\n");
                    }
                }

                if (Input.GetKeyUp(KeyCode.Mouse0))
                    _attachedTransformsByOffset.Clear();

                foreach (var attachedTransformByOffset in _attachedTransformsByOffset)
                {
                    Transform transform = attachedTransformByOffset.Key;
                    Vector3 offset = attachedTransformByOffset.Value;
                    transform.position = Input.mousePosition + offset;
                }
            }
        }

        // Utility
        private bool _isInEditMode;
        private Dictionary<Transform, Vector3> _attachedTransformsByOffset;

    }
}