

function TestNow(cameraFriendlyName, countMax) {
    new WebAppTester().TestNow( "a", "b", "c", TestNowCallback)
}

function TestNowCallback(context, result) {
    // for each of the video clips get the most recent trigger images
    for (i = 0; i < result.length; ++i) {
        g_RecentVideoClips[i] = { "Clip": result[i], "TriggerImages": null };
        GetClipTriggerImages(g_RecentVideoClips[i].Clip, MAX_TRIGGER_IMAGE_COUNT);
    }
}
