import gdown

urls = [
    ("https://drive.google.com/uc?id=1yX33-eNxgJDI5-PQGuzh_xAOVHLqul-p", "SunsetRecording_v2"),
    ("https://drive.google.com/uc?id=1q31m10sBwzVYfvYQAD7rUfW_OZpomFlB", "SunsetRecording_v1")
]

to = "Assets/360_TL/"

for (url, name) in urls:
    gdown.download(url, to + name + ".mp4")