import cv2
import numpy as np

# Initialize video capturer
cap = cv2.VideoCapture(0)

# Set frame width and height
frame_width = int(cap.get(cv2.CAP_PROP_FRAME_WIDTH))
frame_height = int(cap.get(cv2.CAP_PROP_FRAME_HEIGHT))

# Initialize angle for hue rotation
angle = 0

while True:
    # Capture frame
    ret, frame = cap.read()

    # Convert frame to HSV color space
    hsv = cv2.cvtColor(frame, cv2.COLOR_BGR2HSV)

    # Split channels
    h, s, v = cv2.split(hsv)

    # Increment angle
    angle = (angle + 1) % 360

    # Rotate hue channel
    h = (h + angle) % 180

    # Merge channels back to HSV image
    hsv = cv2.merge((h, s, v))

    # Convert back to BGR color space
    result = cv2.cvtColor(hsv, cv2.COLOR_HSV2BGR)

    # Display frame
    cv2.imshow("Webcam", result)

    # Check for user input
    key = cv2.waitKey(1)
    if key == 27: # Esc key
        break

# Release video capturer
cap.release()

# Close all windows
cv2.destroyAllWindows()