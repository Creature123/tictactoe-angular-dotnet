#!/usr/bin/env node
// PostToolUse hook (Edit|MultiEdit|Write).
// Formats the file Claude just changed, deterministically, so formatting is never left to the model:
//   backend/**/*.cs                    -> dotnet format whitespace (folder mode, no build needed)
//   frontend/**/*.{ts,html,scss,css}   -> Prettier, if installed in frontend/node_modules
// Best-effort: always exits 0 so formatting problems never block work.
import { execFileSync } from 'node:child_process';
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';

const run = (cmd, args, cwd) => {
  try {
    execFileSync(cmd, args, { cwd, stdio: 'ignore', timeout: 45_000 });
  } catch {
    /* formatting is best-effort */
  }
};

try {
  const input = JSON.parse(readFileSync(0, 'utf8') || '{}');
  const projectDir = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();
  const filePath = input.tool_input?.file_path;
  if (!filePath) process.exit(0);

  const abs = path.resolve(projectDir, filePath);
  const rel = path.relative(projectDir, abs).split(path.sep).join('/');

  if (rel.startsWith('backend/') && rel.endsWith('.cs')) {
    const backendDir = path.join(projectDir, 'backend');
    run('dotnet', ['format', 'whitespace', backendDir, '--folder', '--include', path.relative(backendDir, abs)], backendDir);
  } else if (rel.startsWith('frontend/') && /\.(ts|html|scss|css)$/.test(rel)) {
    const frontendDir = path.join(projectDir, 'frontend');
    const prettier = path.join(frontendDir, 'node_modules', '.bin', 'prettier');
    if (existsSync(prettier)) run(prettier, ['--write', abs], frontendDir);
  }
} catch {
  /* ignore malformed payloads */
}
process.exit(0);
