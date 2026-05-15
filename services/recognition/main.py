# 图片零样本识别服务。

import io

import uvicorn
from fastapi import FastAPI, HTTPException, UploadFile
from PIL import Image
from transformers import pipeline

app = FastAPI()

print("正在加载 CLIP 模型...")
clf = pipeline(
    "zero-shot-image-classification",
    model="openai/clip-vit-base-patch32",
)
print("识别服务已就绪。")

LABELS = [
    "building",
    "skyscraper",
    "house",
    "mountain",
    "hill",
    "street lamp",
    "bench",
    "car",
    "bus",
    "tree",
    "bridge",
]

CAT = {
    "building": "Building",
    "skyscraper": "Building",
    "house": "Building",
    "mountain": "Terrain",
    "hill": "Terrain",
    "tree": "Vegetation",
}


@app.get("/health")
def health():
    """返回服务健康状态。"""
    return {"status": "ok"}


@app.post("/recognize")
async def recognize(file: UploadFile):
    """识别上传图片中的主要对象。"""
    # 校验上传内容必须是图片。
    if not (file.content_type or "").startswith("image/"):
        raise HTTPException(400, "上传文件不是图片")

    # 读取图片并转换为 RGB，保证 CLIP 输入格式稳定。
    img = Image.open(io.BytesIO(await file.read())).convert("RGB")
    top = clf(img, candidate_labels=LABELS)[0]
    return {
        "name": top["label"],
        "category": CAT.get(top["label"], "StandardProduct"),
        "confidence": round(top["score"], 4),
    }


if __name__ == "__main__":
    # 以本机 8000 端口启动识别服务。
    uvicorn.run(app, host="0.0.0.0", port=8000, log_level="warning")
