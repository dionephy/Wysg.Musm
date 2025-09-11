# workers/embeddings/Dockerfile
FROM python:3.11-slim

RUN apt-get update && apt-get install -y --no-install-recommends \
    git build-essential && rm -rf /var/lib/apt/lists/*

WORKDIR /app
COPY requirements.txt .
RUN pip install -r requirements.txt

COPY embed_phrases.py .

# Default envs can be overridden by docker-compose or CLI
ENV EMBED_MODEL=BAAI/bge-m3
ENV EMBED_BATCH=128

CMD ["python", "embed_phrases.py"]


#GPU (optional): if you want CUDA w/ Docker, 
#switch base image to a CUDA-enabled Python image and run with 
#--gpus all. 
#For bare-metal/WSL2, install the CUDA wheel for Torch 
#(pip install torch --index-url https://download.pytorch.org/whl/cu121) 
#that matches your NVIDIA driver.