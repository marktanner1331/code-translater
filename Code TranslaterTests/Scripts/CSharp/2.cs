
// Initialize video capturer
var cap = cv2.VideoCapture(0);

// Set frame width and height
int frame_width = Int.Parse(cap.get(cv2.CAP_PROP_FRAME_WIDTH));
int frame_height = Int.Parse(cap.get(cv2.CAP_PROP_FRAME_HEIGHT));

// Initialize angle for hue rotation
float angle = 0;

while (true)
{
	// Capture frame
	var retframe = cap.read();
	var ret = retframe.ret;
	var frame = retframe.frame;
	
	// Convert frame to HSV color space
	var hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV);
	
	// Split channels
	hsv = cv2.split(hsv);
	var h = hsv.h;
	var s = hsv.s;
	var v = hsv.v;
	
	// Increment angle
	angle = (angle + 1) % 360;
	
	// Rotate hue channel
	h = (h + angle) % 180;
	
	// Merge channels back to HSV image
	hsv = cv2.merge(h, s, v);
	
	// Convert back to BGR color space
	var result = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR);
	
	// Display frame
	cv2.imshow("Webcam", result);
	
	// Check for user input
	var key = cv2.waitKey(1);
	if (key == 27)
	{
		break;
	}
}

// Release video capturer
cap.release();

// Close all windows
cv2.destroyAllWindows();