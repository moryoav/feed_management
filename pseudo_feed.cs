////////////////////////////////////////////////////////////////////////////////
// PSEUDO CODE FOR A VIDEO FEED APP WITH QUEUE CAPACITY = 30
// - We have 2 APIs to fetch videos (each returns 20 videos):
//       1) fetchInitialVideosFromServer() -> returns (up to 20 videos) + recommendationID
//       2) fetchNextVideosFromServer(recommendationID) -> returns 20 videos (same ID)
// - videoQueue can hold up to 30 items in memory. This allows appending a full
//   batch of 20 without discarding any.
// - However, we only ever *download* up to 2 behind, 1 current, and up to 17 ahead.
// - We remove from the front any videos that are more than 2 behind currentIndex.
// - We only call fetchNextVideosFromServer if there's enough space (>= 20) to
//   store the entire incoming batch.
////////////////////////////////////////////////////////////////////////////////


////////////////////////////////////////////////////////////////////////////////
// DATA STRUCTURES & VARIABLES
////////////////////////////////////////////////////////////////////////////////

videoQueue = []          // Array of up to 30 Video objects in memory
currentIndex = 0         // The "current" video being watched
maxBehind = 2            // Keep at most 2 videos behind in local storage
maxDownloadAhead = 17    // We'll only download up to 17 ahead
maxQueueCapacity = 30    // Memory capacity for videoQueue
isRunning = false        // Controls the background loop

recommendationID = null  // Obtained from the first API call (and reused)

enum DownloadStatus {
  NOT_STARTED,
  DOWNLOADING,
  DOWNLOADED
}

struct Video {
  id              // Unique identifier
  url             // URL to download from
  localPath       // Where it is stored on device (if downloaded)
  downloadStatus  // NOT_STARTED, DOWNLOADING, or DOWNLOADED
}


////////////////////////////////////////////////////////////////////////////////
// MAIN STARTUP / INITIALIZATION
////////////////////////////////////////////////////////////////////////////////

function startApp() {
    // 1) Fetch the initial batch (up to 20 videos + recommendationID)
    (initialVideos, recommendationID) = fetchInitialVideosFromServer()

    // 2) Store them into videoQueue
    videoQueue = initialVideos   // Could be anywhere from 1..20 videos

    // 3) currentIndex = 0 (start watching the first video)
    currentIndex = 0

    // 4) Start background thread for downloading, cleanup, fetching, etc.
    isRunning = true
    startBackgroundThread()
}


////////////////////////////////////////////////////////////////////////////////
// BACKGROUND THREAD
////////////////////////////////////////////////////////////////////////////////

function startBackgroundThread() {
    while isRunning {
        // A) Remove items from the front if they are more than 2 behind currentIndex
        cleanupOldVideos()

        // B) Download needed videos (prioritize next-to-play items)
        downloadVideosIfNeeded()

        // C) Decide if we have enough room to fetch the next 20
        fetchMoreIfNeeded()

        // D) Sleep or wait to avoid tight looping
        sleep(100ms)
    }
}


////////////////////////////////////////////////////////////////////////////////
// A) CLEANUP OLD VIDEOS (more than 2 behind currentIndex)
////////////////////////////////////////////////////////////////////////////////

function cleanupOldVideos() {
    let minAllowedIndex = currentIndex - maxBehind  // e.g. currentIndex - 2
    if minAllowedIndex < 0 then minAllowedIndex = 0

    // While we have items in the front that are below 'minAllowedIndex'
    while videoQueue.size > 0 and (0 < minAllowedIndex) {
        let oldVideo = videoQueue[0]

        // If it's downloaded, delete from disk
        if oldVideo.downloadStatus == DownloadStatus.DOWNLOADED {
            deleteLocalFile(oldVideo.localPath)
        }

        // Remove from the array
        videoQueue.removeAt(0)

        // Adjust currentIndex and minAllowedIndex
        currentIndex--
        minAllowedIndex--
    }

    // Safety check
    if currentIndex < 0 then currentIndex = 0
}


////////////////////////////////////////////////////////////////////////////////
// B) DOWNLOAD MANAGEMENT (SEQUENTIAL)
////////////////////////////////////////////////////////////////////////////////

function downloadVideosIfNeeded() {
    // We'll look from currentIndex forward up to currentIndex + maxDownloadAhead
    // But do not exceed the array bounds
    let endIndex = min(currentIndex + maxDownloadAhead, videoQueue.size - 1)

    for i in range(currentIndex, endIndex + 1):
        let vid = videoQueue[i]
        if vid.downloadStatus == DownloadStatus.NOT_STARTED {
            downloadVideo(vid)
            // Break to ensure only one download at a time in this loop iteration
            break
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// C) FETCH MORE IF NEEDED (only if there's enough space for 20 new items)
////////////////////////////////////////////////////////////////////////////////

function fetchMoreIfNeeded() {
    // Check how many free slots remain in videoQueue (capacity = 30)
    let freeSlots = maxQueueCapacity - videoQueue.size

    // We only call the expensive API if we have room for all 20 new videos
    if freeSlots >= 20 {
        // Also check if the user is somewhat close to the end of the queue
        // (We can pick any heuristic, e.g. "within last 5" or "within last 10".)
        // Or we can simply fetch as soon as there is space. For example:
        if (videoQueue.size - currentIndex) <= 10 {
            // This means fewer than ~10 videos remain ahead of the current

            // Perform the second API call
            let newVideos = fetchNextVideosFromServer(recommendationID)
              // returns exactly 20 new videos

            // Append all 20 videos (we have guaranteed space for them)
            for v in newVideos {
                videoQueue.append(v)
            }
            // Now videoQueue.size has grown by 20 (still <= 30).
            // We do *not* get a new recommendationID from the second call,
            // so we keep using the same one.
        }
    }
}


////////////////////////////////////////////////////////////////////////////////
// ADVANCING TO THE NEXT VIDEO (e.g., user swipes to the next)
////////////////////////////////////////////////////////////////////////////////

function onUserNextVideo() {
    // Move from currentIndex to the next, if possible
    if currentIndex < videoQueue.size - 1 {
        currentIndex++
    }

    // Optional immediate cleanup
    cleanupOldVideos()

    // Optionally trigger next steps (background thread does it anyway)
    downloadVideosIfNeeded()
    fetchMoreIfNeeded()
}


////////////////////////////////////////////////////////////////////////////////
// HELPER FUNCTIONS
////////////////////////////////////////////////////////////////////////////////

// 1) Fetch from server (returns up to 20 videos + a recommendationID).
function fetchInitialVideosFromServer() -> (List<Video>, String) {

    //   return (listOfUpTo20Videos, recommendationID)
    // ...
}

// 2) Fetch from server using the same recommendationID
//    (does NOT return a new ID, we keep using the old one).
function fetchNextVideosFromServer(recommendationID) -> List<Video> {
    //   return exactly 20 videos (or fewer if none left, but typically 20)
    // ...
}


// Download one video file
function downloadVideo(videoItem) {
    videoItem.downloadStatus = DownloadStatus.DOWNLOADING
    // Possibly a blocking or async call:
    //   localPath = doDownloadFile(videoItem.url)
    //   if success:
    //       videoItem.localPath = localPath
    //       videoItem.downloadStatus = DownloadStatus.DOWNLOADED
    //   else:
    //       videoItem.downloadStatus = DownloadStatus.NOT_STARTED
    //       videoItem.localPath = null
}


// Delete file from local storage
function deleteLocalFile(path) {
    // ...
}





