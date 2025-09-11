# -*- coding: utf-8 -*-
# embed_phrases.py
import os, time, psycopg2, numpy as np
from typing import List, Tuple

# Choose a 1024-d model; BGE-M3 works well and outputs 1024 dims
# pip install transformers torch sentencepiece psycopg2-binary
from transformers import AutoTokenizer, AutoModel

MODEL_NAME = os.getenv("EMBED_MODEL", "BAAI/bge-m3")  # 1024-d
DB_DSN     = os.getenv("PG_DSN", "host=localhost dbname=wysg_dev user=postgres password=postgres")

BATCH = int(os.getenv("EMBED_BATCH", "128"))
LANG_HINT = os.getenv("LANG_HINT", "")  # optional

tok = AutoTokenizer.from_pretrained(MODEL_NAME)
mdl = AutoModel.from_pretrained(MODEL_NAME)

def embed_texts(texts: List[str]) -> np.ndarray:
    # mean-pooled CLS-style; adjust per your modelí»s recommended pooling if needed
    with torch.no_grad():
        T = tok(texts, padding=True, truncation=True, return_tensors="pt")
        X = mdl(**T).last_hidden_state  # [B, L, H]
        V = X.mean(dim=1).cpu().numpy() # [B, H]
        # L2-normalize for cosine
        V = V / (np.linalg.norm(V, axis=1, keepdims=True) + 1e-12)
        return V

def fetch_batch(cur, model: str, limit: int) -> List[Tuple[int, str, str]]:
    cur.execute("""
        SELECT p.id, p.text, p.lang
        FROM content.phrase p
        LEFT JOIN content.phrase_embedding e
          ON e.phrase_id = p.id AND e.model = %s
        WHERE e.phrase_id IS NULL
          AND p.active = true
        ORDER BY p.id
        LIMIT %s
    """, (model, limit))
    return cur.fetchall()

def upsert_vectors(cur, rows, vecs, model):
    for (pid, _txt, _lang), v in zip(rows, vecs):
        cur.execute("SELECT content.set_phrase_embedding(%s, %s, %s::vector)",
                    (pid, model, list(v)))  # pgvector accepts Python list

def main():
    import torch  # keep torch import inside main so script can parse without GPU
    conn = psycopg2.connect(DB_DSN)
    conn.autocommit = False
    cur = conn.cursor()

    while True:
        rows = fetch_batch(cur, MODEL_NAME, BATCH)
        if not rows:
            print("No more phrases needing embedding.")
            break

        texts = [r[1] for r in rows]
        vecs = embed_texts(texts)

        upsert_vectors(cur, rows, vecs, MODEL_NAME)
        conn.commit()
        print(f"Embedded {len(rows)} rows.")

        # be nice to the DB
        time.sleep(0.1)

    cur.close(); conn.close()

if __name__ == "__main__":
    main()
