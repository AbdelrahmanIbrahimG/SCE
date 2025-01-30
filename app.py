import cv2
import mediapipe as mp
import numpy as np

#-------------------------------------------------------------------------------------------
def calculate_angle(a,b,c):
    
    radians = np.arctan2(c-b, c-b) - np.arctan2(a-b, a-b)
    angle = np.abs(radians*180.0/np.pi)
        
    return angle

#-------------------------------------------------------------------------------------------
def volume(hand_landmarks, index_tip, wrist):
    index_mid = hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_MCP].y

    angle = calculate_angle(index_tip, index_mid, wrist)

    if angle == 180.0:
         return 'volumeup'
    elif angle == 0.0:
         return 'volumedown'
            
#-------------------------------------------------------------------------------------------
def playback(thumb_tip, index_tip, middle_tip, pinky_tip):
        
    if index_tip < thumb_tip and middle_tip < thumb_tip and pinky_tip > thumb_tip:
        return 'playback'

#-------------------------------------------------------------------------------------------
def skip(hand_landmarks):
    
    tips = [
        hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.RING_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_TIP],
    ]
    mcps = [
        hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.RING_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_MCP],
    ]
    
    # Check if each finger tip is above its corresponding MCP joint
    open_fist = all(tip.y < mcp.y for tip, mcp in zip(tips, mcps))
    
    if open_fist == True:
        return open_fist
    
    return False

#-------------------------------------------------------------------------------------------
def drawback(hand_landmarks):
    
    tips = [
        hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.RING_FINGER_TIP],
        hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_TIP],
    ]
    mcps = [
        hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.RING_FINGER_MCP],
        hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_MCP],
    ]
    
    # Check if each finger tip is below its corresponding MCP joint (closed fist condition)
    closed_fist = all(tip.y > mcp.y for tip, mcp in zip(tips, mcps))
    
    if closed_fist == True:
        return closed_fist

#-------------------------------------------------------------------------------------------

mp_hands = mp.solutions.hands
hands = mp_hands.Hands(static_image_mode=False, max_num_hands=1,
                       min_detection_confidence=0.5, min_tracking_confidence=0.5)

mp_drawing = mp.solutions.drawing_utils

def get_gesture_from_frame(frame):

    image_rgb = cv2.cvtColor(frame, cv2.COLOR_BGR2RGB)

    results = hands.process(image_rgb)

    if results.multi_hand_landmarks:
        for hand_landmarks in results.multi_hand_landmarks:
            mp_drawing.draw_landmarks(
                frame, hand_landmarks, mp_hands.HAND_CONNECTIONS)

            try:

                thumb_tip = hand_landmarks.landmark[mp_hands.HandLandmark.THUMB_TIP].y
                index_tip = hand_landmarks.landmark[mp_hands.HandLandmark.INDEX_FINGER_TIP].y
                middle_tip = hand_landmarks.landmark[mp_hands.HandLandmark.MIDDLE_FINGER_TIP].y
                pinky_tip = hand_landmarks.landmark[mp_hands.HandLandmark.PINKY_TIP].y
                wrist = hand_landmarks.landmark[mp_hands.HandLandmark.WRIST].y

                if skip(hand_landmarks):
                    return "skip"

                elif drawback(hand_landmarks):
                    return "drawback"

                elif index_tip < thumb_tip and middle_tip < thumb_tip and pinky_tip > thumb_tip:
                    return playback(thumb_tip, index_tip, middle_tip, pinky_tip)

                elif index_tip < wrist:
                    return volume(hand_landmarks, index_tip, wrist)

            except Exception as error:
                print(error)
                return "error"

# Testing
# frame =cv2.imread("C:\\Users\\alihi\\ipynb\\Image Processing\\image-processing-project\\testCases\\pause.jpg")
# print(get_gesture_from_frame(frame))