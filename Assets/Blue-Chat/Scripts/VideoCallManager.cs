using System;
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
    private IRtcEngineEx RtcEngineEx;
    private RtcConnection connection;
    private VideoCallRTCEventHandler rtcEventHandler;
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
        RtcEngineEx = RtcEngine.CreateAgoraRtcEngineEx();
        rtcEventHandler = new VideoCallRTCEventHandler(this);
        RtcEngineEx.InitEventHandler(rtcEventHandler);

        RtcEngineContext context = new RtcEngineContext
        {
            appId = appId,
            channelProfile = CHANNEL_PROFILE_TYPE.CHANNEL_PROFILE_LIVE_BROADCASTING,
            audioScenario = AUDIO_SCENARIO_TYPE.AUDIO_SCENARIO_DEFAULT,
            areaCode = AREA_CODE.AREA_CODE_AS,

        };
        var result = RtcEngineEx.Initialize(context);
        Debug.Log("Initialize result : " + result);
        VideoEncoderConfiguration config = new VideoEncoderConfiguration();
        config.dimensions = new VideoDimensions(640, 360);
        config.frameRate = 15;
        config.bitrate = 0;
        RtcEngineEx.SetVideoEncoderConfiguration(config);
        RtcEngineEx.SetClientRole(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
    }

    private void JoinChannel()
    {
        channelName = channelInputField.text;
        connection = new RtcConnection
        {
            channelId = channelName,
            localUid = 0 // Let Agora assign a UID
        };

        ChannelMediaOptions options = new ChannelMediaOptions();
        options.autoSubscribeAudio.SetValue(true);
        options.autoSubscribeVideo.SetValue(true);
        options.publishMicrophoneTrack.SetValue(false);
        options.publishCameraTrack.SetValue(false);
        RtcEngineEx.JoinChannelEx("", connection, options);
        var node = JoinChannelVideo.MakeVideoView(0, channelName);
        LocalVideoSurface.texture = node.GetComponent<RawImage>().texture;
    }
    private void LeaveChannel()
    {
        RtcEngineEx.LeaveChannelEx(connection);
    }

    private void StartPreview()
    {
        RtcEngineEx.StartPreview();
        var node = JoinChannelVideo.MakeVideoView(0, channelName);
        LocalVideoSurface.texture = node.GetComponent<RawImage>().texture;
    }

    private void StopPreview()
    {
        JoinChannelVideo.DestroyVideoView(0);
        RtcEngineEx.StopPreview();
    }

    private void StartPublish()
    {
        var options = new ChannelMediaOptions();
        options.publishMicrophoneTrack.SetValue(true);
        options.publishCameraTrack.SetValue(true);
        options.clientRoleType.SetValue(CLIENT_ROLE_TYPE.CLIENT_ROLE_BROADCASTER);
        var nRet = RtcEngineEx.UpdateChannelMediaOptions(options);
        Debug.Log("UpdateChannelMediaOptions: " + nRet);
    }

    private void StopPublish()
    {
        var options = new ChannelMediaOptions();
        options.publishMicrophoneTrack.SetValue(false);
        options.publishCameraTrack.SetValue(false);
        var nRet = RtcEngineEx.UpdateChannelMediaOptions(options);
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