// GallerySave.m — save a PNG to the iOS Photos library.
//
// Called from GallerySave.cs via DllImport("__Internal"). The path is a
// full POSIX path to a file already written to persistentDataPath by the
// C# side. On success the asset is added to the user's Photos library;
// on failure UnitySendMessage reports the error back to the C# callback.
//
// Requires Photos framework (available since iOS 8; the project min is
// iOS 14+, so the availability check is a formality).

#import <Photos/Photos.h>
#import "UnityInterface.h"

void _GallerySave(const char* path)
{
    if (path == NULL) {
        UnitySendMessage("GallerySave", "OnSaveFailed", "null path");
        return;
    }

    NSString* filePath = [NSString stringWithUTF8String:path];

    // Bail early with a clear message if the file does not exist.
    if (![[NSFileManager defaultManager] fileExistsAtPath:filePath]) {
        UnitySendMessage("GallerySave", "OnSaveFailed",
            "file not found");
        return;
    }

    // Check Photos authorization status first.
    PHAuthorizationStatus status = [PHPhotoLibrary authorizationStatus];
    if (status == PHAuthorizationStatusDenied ||
        status == PHAuthorizationStatusRestricted) {
        UnitySendMessage("GallerySave", "OnSaveFailed",
            "photos access denied");
        return;
    }

    // If authorization has not been requested yet, request it and bail —
    // the user must grant access before we can write. The app should
    // request PHPhotoLibrary.requestAuthorization: elsewhere on first use.
    if (status == PHAuthorizationStatusNotDetermined) {
        UnitySendMessage("GallerySave", "OnSaveFailed",
            "photos access not yet authorized");
        return;
    }

    UIImage* image = [UIImage imageWithContentsOfFile:filePath];
    if (image == nil) {
        UnitySendMessage("GallerySave", "OnSaveFailed",
            "could not decode image");
        return;
    }

    [[PHPhotoLibrary sharedPhotoLibrary] performChanges:^{
        [PHAssetChangeRequest creationRequestForAssetFromImage:image];
    } completionHandler:^(BOOL success, NSError* error) {
        if (success) {
            UnitySendMessage("GallerySave", "OnSaveSuccess", "");
        } else {
            NSString* msg = [error localizedDescription] ?: @"unknown error";
            UnitySendMessage("GallerySave", "OnSaveFailed",
                [msg UTF8String]);
        }
    }];
}
