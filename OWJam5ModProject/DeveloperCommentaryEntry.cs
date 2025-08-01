﻿using NewHorizons.Components.Props;
using Newtonsoft.Json.Linq;
using OWML.Common;
using OWML.ModHelper;
using OWML.Utils;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace OWJam5ModProject
{
    internal class DeveloperCommentaryEntry : MonoBehaviour
    {
        public enum CommentaryAuthor { Walker, Cleric, Jamie, John, Tutorial}

        public const string SIGNAL_FREQUENCY_NAME = "Developer Commentary";
        public const string DEVELOPER_COMMENTARY_OPTION = "developerCommentary";
        const string COMMENTARY_READ_NOTIFICATION = "DEVELOPER_COMMENTARY_READ_NOTIFICATION";
        const string EMISSION_COLOR_PARAMETER = "_EmissionColor";
        const string ALL_COMMENTARY_READ_CONDITION = "Walker_Jam5_AllCommentaryRead";
        const float SIGNAL_VOLUME = 0.05f;

        static int[] entryCounts = new int[4];
        static int[] readEntries = new int[4];
        static int totalEntries;
        static int totalReadEntries;

        [Header("Global Options")]
        [SerializeField] NHCharacterDialogueTree dialogTree;
        [SerializeField] MeshRenderer propRenderer;
        [SerializeField] int propAuthorMaterialIndex = 1;
        [SerializeField] Material[] authorMaterials;
        [SerializeField] float materialFadeMultiplier = 0.25f;
        [SerializeField] float materialFadeDuration = 1;

        [Header("Settings")]
        [Tooltip("The XML file for the commentary dialog, automatically copied to conversation tree")]
        [SerializeField] TextAsset dialogXml;
        [Tooltip("The author of the commentary, automatically sets prop material")]
        [SerializeField] CommentaryAuthor author;
        [Tooltip("The name of the signal emitted by this commentary. Should be the topic discussed by the commentary. Must be unique")]
        [SerializeField] string signalName = "Commentary Topic";
        [Tooltip("The range at which the signal is detected. Leave at default unless you have a reason to change it")]
        [SerializeField] float signalDetectionRange = 50;
        [Tooltip("An array of demonstration components this commentary can activate")]
        [SerializeField] DeveloperCommentaryDemonstration[] demonstrations;

        Signalscope signalscope;
        AudioSignal signal;
        Vector3 initialAttentionPoint;
        bool commentaryRead;
        bool anyEnabled = false;
        bool allEnabled = false;

        void Start()
        {
            signal = OWJam5ModProject.Instance.NewHorizons.SpawnSignal(OWJam5ModProject.Instance, gameObject, "", signalName, SIGNAL_FREQUENCY_NAME, detectionRadius:signalDetectionRange, identificationRadius: -1);
            signal._signalVolume = 0.5f;
            signal.transform.parent = transform;
            signal._owAudioSource.clip = NewHorizons.Utility.Files.AssetBundleUtilities.Load<AudioClip>("planets/assets/walker_jam5_bundle", "Assets/_Bundle/Audio/devCommentary.ogg", OWJam5ModProject.Instance);
            signal._owAudioSource.SetMaxVolume(SIGNAL_VOLUME);

            dialogTree.OnAdvancePage += DialogTree_OnAdvancePage;
            dialogTree.OnEndConversation += DialogTree_OnEndConversation;
            dialogTree.OnStartConversation += DialogTree_OnStartConversation;

            OWJam5ModProject.Instance.OnConfigurationChanged += OnConfigurationChanged;

            initialAttentionPoint = dialogTree._attentionPoint.localPosition;

            UpdateCommentaryRead();

            signalscope = FindObjectOfType<Signalscope>();

            if (author != CommentaryAuthor.Tutorial)
            {
                entryCounts[(int)author]++;
                totalEntries++;
                if (commentaryRead)
                {
                    readEntries[(int)author]++;
                    totalReadEntries++;
                }

            }

            OnConfigurationChanged(OWJam5ModProject.Instance.ModHelper.Config);
        }

        void OnDestroy()
        {
            OWJam5ModProject.Instance.OnConfigurationChanged -= OnConfigurationChanged;
        }

        void OnValidate()
        {
            UpdateAuthorMaterial();

            if (dialogXml != null)
                dialogTree._xmlCharacterDialogueAsset = dialogXml;
        }

        public static void ResetEntryCounts()
        {
            for (int i = 0; i < entryCounts.Length; i++)
            {
                entryCounts[i] = 0;
                readEntries[i] = 0;
            }
            totalEntries = 0;
            totalReadEntries = 0;
        }

        public void MoveAttentionPoint(Transform target)
        {
            dialogTree._attentionPoint.transform.position = target.position;
        }

        public void ResetAttentionPoint()
        {
            dialogTree._attentionPoint.transform.localPosition = initialAttentionPoint;
        }

        private void SetCommentaryEnabled(bool value)
        {
            gameObject.SetActive(value);
            signal.SetSignalActivation(value, 0);
        }

        private void UpdateAuthorMaterial()
        {
            Material[] sharedMaterials = propRenderer.sharedMaterials;
            sharedMaterials[propAuthorMaterialIndex] = authorMaterials[(int)author];
            propRenderer.sharedMaterials = sharedMaterials;
        }

        private void UpdateCommentaryRead(bool setRead = false)
        {
            bool readPrevious = commentaryRead;

            if (setRead && !readPrevious)
            {
                if (author != CommentaryAuthor.Tutorial)
                {
                    readEntries[(int)author]++;
                    totalReadEntries++;

                    if (totalReadEntries == totalEntries)
                        PlayerData.SetPersistentCondition(ALL_COMMENTARY_READ_CONDITION, true);

                    int notificationReadEntries = allEnabled ? totalReadEntries : readEntries[(int)author];
                    int notificationTotalEntries = allEnabled ? totalEntries : entryCounts[(int)author];
                    string notificationString = notificationReadEntries.ToString() + "/" + notificationTotalEntries.ToString() + " " + OWJam5ModProject.Instance.NewHorizons.GetTranslationForUI(COMMENTARY_READ_NOTIFICATION);
                    NotificationData notification = new NotificationData(NotificationTarget.Player, notificationString, 10f);
                    NotificationManager.SharedInstance.PostNotification(notification);
                }

                signal.IdentifySignal();
                commentaryRead = true;
            }
            else
            {
                commentaryRead = PlayerData.KnowsSignal(signal.GetName());
            }

            if (commentaryRead && !readPrevious)
                StartCoroutine(FadeMaterial());
        }

        IEnumerator FadeMaterial()
        {
            Color initialColor = propRenderer.materials[propAuthorMaterialIndex].GetColor(EMISSION_COLOR_PARAMETER);
            Color targetColor = initialColor * materialFadeMultiplier;

            float t = 0;
            while (t < 1)
            {
                propRenderer.materials[propAuthorMaterialIndex].SetColor(EMISSION_COLOR_PARAMETER, Color.Lerp(initialColor, targetColor, t));
                t += Time.deltaTime / materialFadeDuration;

                yield return new WaitForEndOfFrame();
            }

            propRenderer.materials[propAuthorMaterialIndex].SetColor(EMISSION_COLOR_PARAMETER, targetColor);
        }

        private void OnConfigurationChanged(IModConfig config)
        {
            //Determine if this one should be active
            string configStr = config.GetSettingsValue<string>(DEVELOPER_COMMENTARY_OPTION).ToLower();
            string authorStr = author.ToString().ToLower();
            bool commentaryEnabled = configStr.Equals("all") || configStr.Contains(authorStr) || (author == CommentaryAuthor.Tutorial && configStr != "none");

            //Set this one up
            SetCommentaryEnabled(commentaryEnabled);

            //See if we need to alter the frequency
            bool anyEnabledBefore = anyEnabled;
            anyEnabled = !configStr.Equals("none");
            if (anyEnabledBefore != anyEnabled)
            {
                if (anyEnabled)
                    signal.IdentifyFrequency();
                else
                {
                    PlayerData.ForgetFrequency(signal._frequency);
                    signalscope.SelectFrequency(SignalFrequency.Traveler);
                }
            }

            allEnabled = configStr.Equals("all");
        }

        private void DialogTree_OnStartConversation()
        {
            RequirementsScreenPrompt.CommentaryDialogStarted();
        }

        private void DialogTree_OnEndConversation()
        {
            ResetAttentionPoint();
            UpdateCommentaryRead(true);
            RequirementsScreenPrompt.CommentaryDialogEnded();
        }

        private void DialogTree_OnAdvancePage(string nodeName, int pageNum)
        {
            foreach (DeveloperCommentaryDemonstration demonstration in demonstrations)
            {
                demonstration.CheckActivation(this, dialogTree);
                demonstration.CheckDeactivation(this, dialogTree);
            }
        }
    }
}
