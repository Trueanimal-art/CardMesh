# Card Mesh: AdMob release checklist

The project uses Google Mobile Ads Unity Plugin 11.3.0. Development builds
use Google's official test App ID and test interstitial/rewarded ad unit IDs.
Release Android builds are intentionally blocked until the production IDs are
created in the owner's AdMob account and entered in
`Assets/Scripts/Ads/AdMobConfiguration.cs`.

## Before the first production AAB

1. In AdMob, register the Android app with package
   `com.onetapgarage.cardmesh`.
2. Create one interstitial unit and one rewarded unit.
3. Replace all three empty `ProductionAndroid*` values in
   `AdMobConfiguration.cs`. The App ID contains `~`; ad unit IDs contain `/`.
4. In AdMob **Privacy & messaging**, publish the European regulations message
   and provide the privacy-policy URL. The in-game settings screen already
   exposes the UMP privacy-options entry point.
5. Link the AdMob app to its Google Play listing. Never click live ads while
   testing; use development builds or configured test devices.
6. In Play Console, declare that the app contains ads, complete Data safety,
   target audience, content rating, and privacy policy. The current code assumes
   a 13+ audience and does not enable child-directed treatment.
7. Add the developer website to the Play listing and publish the exact
   `app-ads.txt` line supplied by AdMob at the website root.
8. Configure AdMob blocking controls/max content rating as required by the
   final IARC rating. Code currently requests a `G` maximum ad content rating.
9. Configure the upload keystore locally and build the release AAB from
   **Tools > Card Mesh > Android > Build Release AAB**.

## Placement rules implemented

- Interstitials retain the existing 120-second cooldown and are requested only
  at existing game transitions/restart thresholds, never at application launch.
- Rewarded ads are user-initiated and grant `+10` moves only after Google's
  reward callback. Closing without earning the reward grants nothing.
- A failed/unavailable ad never blocks gameplay.
- Ads are loaded only after the UMP consent update/form allows ad requests.
- Full-screen ad objects are single-use, destroyed after close/failure, and the
  next ad is preloaded without an infinite retry loop.

## Official references

- https://developers.google.com/admob/unity/quick-start
- https://developers.google.com/admob/unity/privacy
- https://developers.google.com/admob/unity/interstitial
- https://developers.google.com/admob/unity/rewarded
- https://developers.google.com/admob/unity/test-ads
- https://developers.google.com/admob/unity/privacy/play-data-disclosure
- https://support.google.com/googleplay/android-developer/answer/9857753
- https://support.google.com/googleplay/android-developer/answer/9893335
- https://support.google.com/googleplay/android-developer/answer/10787469
- https://support.google.com/admob/answer/9363762
