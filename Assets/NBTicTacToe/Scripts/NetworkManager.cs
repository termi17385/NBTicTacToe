using NBTicTacToe.Game.Manager;
using Riptide;
using Riptide.Utils;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

enum MessageID : ushort
{
    FromClient,
    ToClient,
    ToClientCB,
    FromClientCB,
    Reset
}

public class NetworkManager : MonoBehaviour
{
    [Header("Connection UI")]
    [SerializeField] private TMP_InputField connectionField;
    [SerializeField] private Button joinButton, hostButton, leaveButton;

    public static NetworkManager Instance;

    const string LOCALHOST = "127.0.0.1";
    const int port = 7777;

    public Server Server;
    Client Client;

    public bool IgnoreFirstCallBack { get; private set; }
    public bool IgnoreFirstCallBackCB { get; private set; }

    private void Awake() => Instance = this;

    private void Start()
    {
        RiptideLogger.Initialize(Debug.Log, Debug.Log, Debug.LogWarning, Debug.LogError, true);

        Server = new Server();
        Server.ClientConnected += Server_ClientConnected;
        Server.ClientDisconnected += Server_ClientDisconnected;
        
        Client = new Client();
        Client.Connected += Client_Connected;
        Client.Disconnected += Client_Disconnected;

        joinButton.onClick.AddListener(JoinGame);
        leaveButton.onClick.AddListener(LeaveGame);
        hostButton.onClick.AddListener(HostServer);

        GameManager.Instance.TurnHandler += OnTurnMade;
        GameManager.Instance.OnStarted += Game_OnStarted;
        GameManager.Instance.OnReset += Instance_OnReset;
    }

    private void Instance_OnReset()
    {
        Message message = Message.Create(MessageSendMode.Reliable, MessageID.Reset);
        Server.SendToAll(message);
    }

    [MessageHandler((ushort)MessageID.Reset)] public static void ResetGame(Message message)
    {
        if(Instance.Server.IsRunning) return;
        GameManager.Instance.ResetBoards();
    }

    private void Game_OnStarted(Vector2Int _position)
    {
        if (IgnoreFirstCallBack)
        {
            IgnoreFirstCallBackCB = false;
        }
        else
        {
            Message message = Server.IsRunning ? Message.Create(MessageSendMode.Reliable, MessageID.ToClientCB)
                : Message.Create(MessageSendMode.Reliable, MessageID.FromClientCB);

            message.AddVector2((Vector2)_position);

            if (Server.IsRunning) Server.SendToAll(message);
            else Client.Send(message);
        }
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        GameManager.paused = Server.IsRunning ? gm.GetCurrentTurn != Turn.CROSS : gm.GetCurrentTurn != Turn.NAUGHT;
    }

    private void OnTurnMade(Vector2Int _position)
    {
        if(IgnoreFirstCallBack)
        {
            IgnoreFirstCallBack = false;
        }
        else
        {
            Message message = Server.IsRunning ? Message.Create(MessageSendMode.Reliable, MessageID.ToClient)
                : Message.Create(MessageSendMode.Reliable, MessageID.FromClient);

            message.AddVector2((Vector2)_position);
        
            if (Server.IsRunning) Server.SendToAll(message);
            else Client.Send(message);
        }
    }

    private void FixedUpdate()
    {
        Server.Update();
        Client.Update();
    }

    private void HostServer()
    {
        Server.Start(port, 2);
        Client.Connect(LOCALHOST + ":" + port);
    }
    
    private void JoinGame()
    {
        string ipAddress = connectionField.text + ":" + port;
        Client.Connect(ipAddress);
    }

    private void LeaveGame()
    {
        Client.Disconnect();
        Application.Quit();
    }

    #region Standard Gameplay
    [MessageHandler((ushort)MessageID.ToClient)] public static void RecieveTurn(Message _message)
    {
        if (Instance.Server.IsRunning) return;

        Vector2 v = _message.GetVector2();
        Vector2Int id = new((int)v.x, (int)v.y);
        
        Instance.IgnoreFirstCallBack = true;
        GameManager.Instance.TileInteraction(id);
    }
    
    [MessageHandler((ushort)MessageID.FromClient)] public static void RecieveTurn(ushort _from, Message _message)
    {
        Vector2 v = _message.GetVector2();
        Vector2Int id = new((int)v.x, (int)v.y);

        Instance.IgnoreFirstCallBack = true;
        GameManager.Instance.TileInteraction(id);
    }
    #endregion

    [MessageHandler((ushort)MessageID.ToClientCB)] public static void RecieveTurnCB(Message _message)
    {
        if (Instance.Server.IsRunning) return;

        Vector2 v = _message.GetVector2();
        Vector2Int id = new((int)v.x, (int)v.y);

        Instance.IgnoreFirstCallBackCB = true;
        GameManager.Instance.SetBoard(id);
    }

    [MessageHandler((ushort)MessageID.FromClientCB)] public static void RecieveTurnCB(ushort _from, Message _message)
    {
        Vector2 v = _message.GetVector2();
        Vector2Int id = new((int)v.x, (int)v.y);

        Instance.IgnoreFirstCallBackCB = true;
        GameManager.Instance.SetBoard(id);
    }

    private void Client_Disconnected(object sender, DisconnectedEventArgs e)
    {
    }
    private void Client_Connected(object sender, System.EventArgs e)
    {
    }
    private void Server_ClientDisconnected(object sender, ServerDisconnectedEventArgs e)
    {
    }
    private void Server_ClientConnected(object sender, ServerConnectedEventArgs e)
    {
    }
}
