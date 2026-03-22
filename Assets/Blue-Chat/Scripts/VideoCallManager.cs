using System;
using System.Threading.Tasks;
using Agora.Rtc;
using Agora_RTC_Plugin.API_Example.Examples.Basic.JoinChannelVideo;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class VideoCallManager : MonoBehaviour
{
    #region  SERIALIZED FIELDS
    [SerializeField] private string appId;
    [SerializeField] private string channelName;
    [SerializeField] private TMP_InputField channelInputField;
    [SerializeField] private Button joinButton, leaveButton;
    [SerializeField] private Button startPreviewButton, stopPreviewButton;
    [SerializeField] private Button startPublishButton, stopPublishButton;
    [SerializeField] private RawImage localVideoSurface;
    [SerializeField] private RawImage remoteVideoSurface;
    #endregion

    #region PRIVATE FIELDS
    private IRtcEngineEx RtcEngine;
    private RtcConnection connection;
    private VideoCallRTCEventHandler rtcEventHandler;
    private ChannelMediaOptions options;
    #endregion

    #region PUBLIC PROPERTIES
    public RawImage LocalVideoSurface => localVideoSurface;
    public RawImage RemoteVideoSurface => remoteVideoSurface;
    public string GetChannelName() => channelName;
    #endregion

    #region UNITY METHODS
    void Awake()
    {
        joinButton.onClick.AddListener(JoinChannel);
        leaveButton.onClick.AddListener(LeaveChannel);
        startPreviewButton.onClick.AddListener(StartPreview);
        stopPreviewButton.onClick.AddListener(StopPreview);
        startPublishButton.onClick.AddListener(StartPublish);
        stopPublishButton.onClick.AddListener(StopPublish);
    }

    void OnDestroy()
    {
        joinButton.onClick.RemoveListener(JoinChannel);
        leaveButton.onClick.RemoveListener(LeaveChannel);
        startPreviewButton.onClick.RemoveListener(StartPreview);
        stopPreviewButton.onClick.RemoveListener(StopPreview);
        startPublishButton.onClick.RemoveListener(StartPublish);
        stopPublishButton.onClick.RemoveListener(StopPublish);
    }

    void Start()
    {
        channelInputField.text = channelName;
        InitAgoraEngine();
    }
    #endregion

    #region PUBLIC METHODS  
    public void OnJoinChannelSuccess()
    {
    }

    public void OnLeaveChannel()
    {
    }
    #endregion

    #region PRIVATE METHODS
    private void InitAgoraEngine()
    {
        RtcEngineContext context = new RtcEngineContext();
        context.appId = appId;
        context.channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING;
        context.audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT;
        RtcEngine = Agora.Rtc.RtcEngine.CreateAgoraRtcEngineEx();

        var result = RtcEngine.Initialize(context);
        Debug.Log("Initialize result : " + result);
        Debug.Log("App Id: " + appId);

        RtcEngine.InitEventHandler(new VideoCallRTCEventHandler(this));
        RtcEngine.EnableAudio();
        RtcEngine.EnableVideo();

        VideoEncoderConfiguration config = new VideoEncoderConfiguration();
        config.dimensions = new VideoDimensions(640, 360);
        config.frameRate = 15;
        config.bitrate = 0;
        RtcEngine.SetVideoEncoderConfiguration(config);
        RtcEngine.SetChannelProfile(CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING);
        RtcEngine.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
    }

    private void JoinChannel()
    {
        Debug.Log("Joining channel: " + channelName);
        connection = new RtcConnection(channelName, 0);
        options = new ChannelMediaOptions();
        options.autoSubscribeAudio.SetValue(true);
        options.autoSubscribeVideo.SetValue(true);
        options.publishMicrophoneTrack.SetValue(false);
        options.publishCameraTrack.SetValue(false);
        options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        RtcEngine.JoinChannelEx("", connection, options);
        var node = JoinChannelVideo.MakeVideoView(0, channelName);
        LocalVideoSurface.texture = node.GetComponent<RawImage>().texture;
    }
    private void LeaveChannel()
    {
        RtcEngine.LeaveChannelEx(connection);
    }

    private void StartPreview()
    {
        RtcEngine.StartPreview();
        var node = JoinChannelVideo.MakeVideoView(0, channelName);
        LocalVideoSurface.texture = node.GetComponent<RawImage>().texture;
    }

    private void StopPreview()
    {
        JoinChannelVideo.DestroyVideoView(0);
        RtcEngine.StopPreview();
    }

    private void StartPublish()
    {
        options.publishMicrophoneTrack.SetValue(true);
        options.publishCameraTrack.SetValue(true);
        options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        var nRet = RtcEngine.UpdateChannelMediaOptionsEx(options, connection);
        Debug.Log("UpdateChannelMediaOptions: " + nRet);
    }

    private void StopPublish()
    {
        options.publishMicrophoneTrack.SetValue(false);
        options.publishCameraTrack.SetValue(false);
        var nRet = RtcEngine.UpdateChannelMediaOptionsEx(options, connection);
        Debug.Log("UpdateChannelMediaOptions: " + nRet);
    }
    #endregion
}
#region  Event Handler for Agora RTC Engine
public class VideoCallRTCEventHandler : IRtcEngineEventHandler
{
    private VideoCallManager manager;

    public VideoCallRTCEventHandler(VideoCallManager manager)
    {
        this.manager = manager;
    }

    public override void OnActiveSpeaker(RtcConnection connection, uint uid)
    {
        base.OnActiveSpeaker(connection, uid);
    }

    public override void OnJoinChannelSuccess(RtcConnection connection, int elapsed)
    {
        Debug.Log($"Joined channel {connection.channelId} with UID {connection.localUid}");
        manager.OnJoinChannelSuccess();
    }

    public override void OnUserJoined(RtcConnection connection, uint uid, int elapsed)
    {
        Debug.Log($"User {uid} joined the channel {connection.channelId}");
        var node = JoinChannelVideo.MakeVideoView(uid, manager.GetChannelName());
        manager.RemoteVideoSurface.texture = node.GetComponent<RawImage>().texture;
    }

    public override void OnUserOffline(RtcConnection connection, uint uid, USER_OFFLINE_REASON_TYPE reason)
    {
        Debug.Log($"User {uid} left the channel {connection.channelId} (reason: {reason})");
        JoinChannelVideo.DestroyVideoView(uid);
        manager.RemoteVideoSurface.texture = null;
    }

    public override void OnLeaveChannel(RtcConnection connection, RtcStats stats)
    {
        Debug.Log($"Left channel {connection.channelId}");
        JoinChannelVideo.DestroyVideoView(0);
        manager.LocalVideoSurface.texture = null;
        manager.OnLeaveChannel();
    }
}
#endregion