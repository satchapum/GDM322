using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode;
using QFSW.QC;
using TMPro;
using System;
using Unity.Mathematics;


public class LoginManagerScript : MonoBehaviour
{
    public TMP_InputField characterIdInputField;
    public List<uint> AlternativePlayerPrefabs;

    public TMP_InputField userNameInputField;
    private bool isApproveConnection = false;
    [Command("set-approve")]

    public GameObject loginPanel;
    public GameObject leaveButton;

    private void Start()
    {
        NetworkManager.Singleton.OnServerStarted += HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += HandleClientDisconnect;
        loginPanel.SetActive(true);
        leaveButton.SetActive(false);
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        Debug.Log("HandleClientDisconnect = " + clientId);
        if (NetworkManager.Singleton.IsHost) { }
        else if (NetworkManager.Singleton.IsClient) { Leave(); }
    }
    public void Leave()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
            NetworkManager.Singleton.ConnectionApprovalCallback -= ApprovalCheck;

        }

        else if (NetworkManager.Singleton.IsClient)
        {
            NetworkManager.Singleton.Shutdown();
        }

        loginPanel.SetActive(true);
        leaveButton.SetActive(false);

    }
    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log("HandleHandleClientConnected = " + clientId);
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            loginPanel.SetActive(false);
            leaveButton.SetActive(true);
        }
    }

    private void HandleServerStarted()
    {
        Debug.Log("HandleServerStarted");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton == null) { return; }
        NetworkManager.Singleton.OnServerStarted -= HandleServerStarted;
        NetworkManager.Singleton.OnClientConnectedCallback -= HandleClientConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
    }

    public bool SetIsApproveConnection()
    {
        isApproveConnection = !isApproveConnection;
        return isApproveConnection;
    }
    public void Host()
    {
        NetworkManager.Singleton.ConnectionApprovalCallback = ApprovalCheck;
        NetworkManager.Singleton.StartHost();
        Debug.Log("Start host");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        // The client identifier to be authenticated
        var clientId = request.ClientNetworkId;

        // Additional connection data defined by user code
        var connectionData = request.Payload;

        int byteLength = connectionData.Length;
        bool isApprove = false;
        int characterPrefabIndex = 0;
        if (byteLength > 0)
        {
            string combinedString = System.Text.Encoding.ASCII.GetString(connectionData,0,byteLength);
            string[] extractedString = HelperScript.ExtractStrings(combinedString);
            for (int i = 0; i < extractedString.Length; i++)
            {
                if (i == 0)
                {
                    string clientData = extractedString[i];
                    string hostData = userNameInputField.GetComponent<TMP_InputField>().text;
                    isApprove = ApproveConnection(clientData, hostData);
                }
                else
                {
                    characterPrefabIndex = int.Parse(extractedString[i]);
                }
            }
        }
        else
        {
            //server
            if (NetworkManager.Singleton.IsHost)
            {
                string characterId = characterIdInputField.GetComponent<TMP_InputField>().text;
                characterPrefabIndex = int.Parse(characterId);
            }
        }
        // Your approval logic determines the following values
        response.Approved = isApprove;
        response.CreatePlayerObject = true;

        // The Prefab hash value of the NetworkPrefab, if null the default NetworkManager player Prefab is used
        //response.PlayerPrefabHash = null;

        response.PlayerPrefabHash = AlternativePlayerPrefabs[characterPrefabIndex];

        // Position to spawn the player object (if null it uses default of Vector3.zero)
        response.Position = Vector3.zero;

        NetworkLog.LogInfoServer("SpanwnPos of " + clientId + " is " + response.Position.ToString());
        // Rotation to spawn the player object (if null it uses the default of Quaternion.identity)

        response.Rotation = Quaternion.identity;

        NetworkLog.LogInfoServer("SpanwnRot of " + clientId + " is " + response.Rotation.ToString());

        SetSpawnLocation(clientId, response);

        // If response.Approved is false, you can provide a message that explains the reason why via ConnectionApprovalResponse.Reason
        // On the client-side, NetworkManager.DisconnectReason will be populated with this message via DisconnectReasonMessage
        response.Reason = "Some reason for not approving the client";

        // If additional approval steps are needed, set this to true until the additional steps are complete
        // once it transitions from true to false the connection approval response will be processed.
        response.Pending = false;
    }

    private void SetSpawnLocation(ulong clientId, NetworkManager.ConnectionApprovalResponse response)
    {
        Vector3 spawnPos = Vector3.zero;
        Quaternion spawnRot = Quaternion.identity;
        //server
        if(clientId == NetworkManager.Singleton.LocalClientId)
        {
            spawnPos = new Vector3(-2f, 0f, 0f);
            spawnRot = Quaternion.Euler(0f, 135f, 0f);
        }
        else
        {
            switch(NetworkManager.Singleton.ConnectedClients.Count)
            {
                case 1:
                    spawnPos = new Vector3(0f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f,180f, 0f);
                    break;
                case 2:
                    spawnPos = new Vector3(2f, 0f, 0f);
                    spawnRot = Quaternion.Euler(0f,225f, 0f);
                    break;
            }
        }
        response.Position = spawnPos;
        response.Rotation = spawnRot;
    }

    public void Client()
    {
        string username = userNameInputField.GetComponent<TMP_InputField>().text;
        string characterId = characterIdInputField.GetComponent<TMP_InputField>().text;
        string[] inputFields = { username, characterId };
        string clientData = HelperScript.CombineStrings(inputFields);
        NetworkManager.Singleton.NetworkConfig.ConnectionData = System.Text.Encoding.ASCII.GetBytes(clientData);
        NetworkManager.Singleton.StartClient();
        Debug.Log("Start client");
    }

    public bool ApproveConnection(string clientData, string hostData)
    {
        bool isApprove = System.String.Equals(clientData.Trim(), hostData.Trim()) ? false : true;
        Debug.Log("isApprove = " + isApprove);
        return isApprove;
    }
}
