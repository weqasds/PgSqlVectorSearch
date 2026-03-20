"""Download sample documents for RAG testing.

This script supports two modes (use --mode):
  - huggingface : download text-based datasets from HuggingFace Datasets
  - arxiv       : download PDF papers from arXiv

The downloaded files are stored under `testdata/documents/` with per-source subfolders.

Example usage:
  python download_documents.py --mode huggingface --dataset hotpot_qa --limit 50
  python download_documents.py --mode arxiv --ids 2103.00020 2307.12345
  python download_documents.py --mode all

Requirements:
  pip install datasets requests tqdm
"""

from __future__ import annotations

import argparse
import json
import os
from pathlib import Path
from typing import List, Optional


def ensure_dir(path: Path) -> Path:
    path.mkdir(parents=True, exist_ok=True)
    return path


def download_huggingface_dataset(dataset_name: str, split: str, limit: Optional[int], out_dir: Path, config: Optional[str] = None) -> None:
    """Download text examples from a HuggingFace dataset and save each example as a text file."""

    try:
        from datasets import load_dataset
    except ImportError as exc:
        raise RuntimeError(
            "Missing dependency 'datasets'. Install with: pip install datasets"
        ) from exc

    # Some datasets require a named configuration (e.g. hotpot_qa uses "distractor" / "fullwiki").
    load_kwargs = {}
    if config:
        load_kwargs["name"] = config

    ds = load_dataset(dataset_name, split=split, **load_kwargs)

    out_dir = ensure_dir(out_dir)

    # Determine primary text field(s) automatically
    # Use common fields: "context", "paragraphs", "document", "text", "article"

    candidate_fields = [
        "text",
        "context",
        "paragraphs",
        "article",
        "document",
        "passage",
        "question",
        "answer",
    ]

    # If dataset contains nested fields, we will attempt best effort
    def extract_text(record: dict) -> str:
        # Handle hotpot_qa style context structure, where "context" is a dict {title: [...], sentences: [[...], ...]}
        if "context" in record and isinstance(record.get("context"), dict):
            context = record.get("context")
            if isinstance(context, dict) and "title" in context and "sentences" in context:
                title = context.get("title")
                if isinstance(title, list):
                    title = " / ".join(str(x) for x in title if x is not None)
                sentences = context.get("sentences")
                if isinstance(sentences, list):
                    flat = []
                    for part in sentences:
                        if isinstance(part, list):
                            flat.extend(part)
                        else:
                            flat.append(str(part))
                    sentences_text = "\n".join(str(x) for x in flat if x is not None)
                else:
                    sentences_text = str(sentences)
                return f"{title}\n\n{sentences_text}".strip()

        # Special-case common QA dataset formats (e.g. hotpot_qa older schemas)
        if "title" in record and record.get("sentences"):
            title = record.get("title")
            if isinstance(title, list):
                title = " / ".join(str(x) for x in title if x is not None)
            sentences = record.get("sentences")
            if isinstance(sentences, list):
                # sentences may be list[list[str]]
                flat = []
                for part in sentences:
                    if isinstance(part, list):
                        flat.extend(part)
                    else:
                        flat.append(str(part))
                sentences_text = "\n".join(str(x) for x in flat if x is not None)
            else:
                sentences_text = str(sentences)

            return f"{title}\n\n{sentences_text}".strip()

        for key in candidate_fields:
            if key in record and record[key] is not None:
                val = record[key]
                if isinstance(val, list):
                    return "\n\n".join(str(x) for x in val if x is not None)
                return str(val)

        # Fallback: join all string values
        parts = []
        for k, v in record.items():
            if isinstance(v, str) and v.strip():
                parts.append(f"{k}: {v}")
        return "\n".join(parts)

    total = len(ds)
    if limit is None or limit <= 0 or limit > total:
        limit = total

    print(f"Downloading {limit} examples from dataset '{dataset_name}' split '{split}' into {out_dir}")

    for idx, record in enumerate(ds.shuffle(seed=42).select(range(limit))):
        text = extract_text(record)
        if not text.strip():
            continue

        file_name = f"{idx:05d}.txt"
        path = out_dir / file_name
        with path.open("w", encoding="utf-8") as f:
            f.write(text)

    meta = {
        "dataset": dataset_name,
        "split": split,
        "count": limit,
    }
    (out_dir / "_metadata.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")

    print(f"Saved {limit} documents to {out_dir}")


def download_arxiv_pdfs(arxiv_ids: List[str], out_dir: Path) -> None:
    """Download arXiv PDFs for the given IDs."""

    import requests
    from tqdm import tqdm

    out_dir = ensure_dir(out_dir)

    for arxiv_id in arxiv_ids:
        normalized = arxiv_id.strip().replace("/", "_")
        pdf_name = f"{normalized}.pdf"
        pdf_path = out_dir / pdf_name
        if pdf_path.exists():
            print(f"Skipping existing {pdf_path}")
            continue

        url = f"https://arxiv.org/pdf/{arxiv_id}.pdf"
        print(f"Downloading {arxiv_id} from {url}")
        resp = requests.get(url, stream=True, timeout=60)
        if resp.status_code != 200:
            print(f"  -> failed ({resp.status_code})")
            continue

        with pdf_path.open("wb") as f:
            for chunk in resp.iter_content(chunk_size=1024 * 256):
                if chunk:
                    f.write(chunk)

        print(f"  -> saved to {pdf_path}")

    meta = {
        "source": "arxiv",
        "ids": arxiv_ids,
        "count": len(arxiv_ids),
    }
    (out_dir / "_metadata.json").write_text(json.dumps(meta, ensure_ascii=False, indent=2), encoding="utf-8")


def main() -> None:
    parser = argparse.ArgumentParser(description="Download documents for RAG testing")
    parser.add_argument(
        "--mode",
        choices=["huggingface", "arxiv", "all"],
        default="all",
        help="Which downloader to run",
    )
    parser.add_argument(
        "--dataset",
        default="hotpot_qa",
        help="HuggingFace dataset to download (default: hotpot_qa)",
    )
    parser.add_argument(
        "--split",
        default="train",
        help="Dataset split to download (default: train)",
    )
    parser.add_argument(
        "--limit",
        type=int,
        default=50,
        help="Maximum number of examples to save (default: 50)",
    )
    parser.add_argument(
        "--ids",
        nargs="*",
        default=[
            "2103.00020",
            "2203.03030",
            "2307.12345",
        ],
        help="List of arXiv IDs to download (default: small sample)",
    )
    parser.add_argument(
    "--config",
    default="distractor",
    help="Dataset config (e.g. distractor, fullwiki)"
    )

    args = parser.parse_args()

    base_dir = Path(__file__).resolve().parents[1] / "testdata" / "documentation"
    ensure_dir(base_dir)

    if args.mode in ("huggingface", "all"):
        hf_dir = base_dir / "huggingface" / args.dataset / args.split
        download_huggingface_dataset(args.dataset, args.split, args.limit, hf_dir, args.config)

    if args.mode in ("arxiv", "all"):
        arxiv_dir = base_dir / "arxiv"
        download_arxiv_pdfs(args.ids, arxiv_dir)


if __name__ == "__main__":
    main()
