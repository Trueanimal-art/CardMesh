#!/usr/bin/env python3
"""Package a Card Mesh WebGL build for a claude.ai artifact.

Artifact hosting limits a binary file to 15 MB and serves only standard
media types, so every Unity build file is split into <=12 MB ".bin" chunks.
The page downloads the chunks, glues them into Blob URLs and hands those to
the Unity loader (the build uses decompression fallback, so no
Content-Encoding headers are needed).

Usage: python3 Tools/package_webgl_artifact.py Builds/WebGL/Preview Builds/WebGL/Artifact
"""
import json
import pathlib
import shutil
import sys

CHUNK = 12 * 1024 * 1024


def main() -> None:
    src = pathlib.Path(sys.argv[1])
    out = pathlib.Path(sys.argv[2])
    build = src / "Build"
    loader = next(build.glob("*.loader.js"))
    parts = {
        "data": next(build.glob("*.data*")),
        "framework": next(build.glob("*.framework.js*")),
        "code": next(build.glob("*.wasm*")),
    }

    if out.exists():
        shutil.rmtree(out)
    (out / "game").mkdir(parents=True)
    shutil.copy(loader, out / "game" / "loader.js")

    manifest = {}
    for key, path in parts.items():
        blob = path.read_bytes()
        names = []
        for index in range(0, len(blob), CHUNK):
            name = f"game/{key}.{index // CHUNK}.bin"
            (out / name).write_bytes(blob[index:index + CHUNK])
            names.append(name)
        manifest[key] = {"parts": names, "size": len(blob)}

    template = (src / "index.html").read_text(encoding="utf-8")
    template = template.replace('<script src="Build/' + loader.name + '"></script>', '<script src="game/loader.js"></script>')
    hook = """<script>
    window.CARDMESH_PARTS = %s;
    window.CARDMESH_BEFORE_START = function (config) {
      var total = 0, loaded = 0, keys = Object.keys(window.CARDMESH_PARTS);
      keys.forEach(function (k) { total += window.CARDMESH_PARTS[k].size; });
      var fill = document.getElementById("fill");
      var status = document.getElementById("status");
      function fetchPart(name) {
        return fetch(name).then(function (r) {
          if (!r.ok) { throw new Error(name + ": " + r.status); }
          return r.arrayBuffer();
        }).then(function (buf) {
          loaded += buf.byteLength;
          fill.style.width = Math.round(loaded / total * 70) + "%%";
          status.textContent = "Загрузка… " + Math.round(loaded / 1048576) + " из " + Math.round(total / 1048576) + " МБ";
          return buf;
        });
      }
      function glue(key) {
        return Promise.all(window.CARDMESH_PARTS[key].parts.map(fetchPart)).then(function (bufs) {
          var size = 0; bufs.forEach(function (b) { size += b.byteLength; });
          var joined = new Uint8Array(size), offset = 0;
          bufs.forEach(function (b) { joined.set(new Uint8Array(b), offset); offset += b.byteLength; });
          return joined.buffer;
        });
      }
      return Promise.all(keys.map(glue)).then(function (buffers) {
        // Serve the glued files to the Unity loader through fetch() itself, no blob: URLs.
        var virtual = {};
        keys.forEach(function (k, i) { virtual["cardmesh-virtual/" + k] = buffers[i]; });
        var realFetch = window.fetch.bind(window);
        window.fetch = function (input, init) {
          var url = typeof input === "string" ? input : (input && input.url) || "";
          for (var name in virtual) {
            if (url.indexOf(name) >= 0) {
              return Promise.resolve(new Response(virtual[name].slice(0), { status: 200, headers: { "Content-Type": "application/octet-stream" } }));
            }
          }
          return realFetch(input, init);
        };
        config.dataUrl = "cardmesh-virtual/data";
        config.frameworkUrl = "cardmesh-virtual/framework";
        config.codeUrl = "cardmesh-virtual/code";
        status.textContent = "Запуск…";
        return config;
      });
    };
  </script>
  <script src="game/loader.js"></script>""" % json.dumps(manifest)
    template = template.replace('<script src="game/loader.js"></script>', hook, 1)
    (out / "index.html").write_text(template, encoding="utf-8")

    total = sum(p.stat().st_size for p in out.rglob("*") if p.is_file())
    print(f"packaged {sum(len(m['parts']) for m in manifest.values())} chunks, {total / 1048576:.1f} MB -> {out}")


if __name__ == "__main__":
    main()
