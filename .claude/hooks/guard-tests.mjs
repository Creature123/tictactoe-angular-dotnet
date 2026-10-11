#!/usr/bin/env node
// PreToolUse hook (Edit|MultiEdit|Write).
// While `.claude/tests.lock` exists, block any edit to test files so the AI cannot
// "get to green" by weakening, skipping or rewriting tests. Exit code 2 = block and
// feed the message back to Claude. Any other outcome lets the normal permission flow continue.
//
//   Lock tests:   touch .claude/tests.lock
//   Unlock tests: rm .claude/tests.lock
import { existsSync, readFileSync } from 'node:fs';
import path from 'node:path';

try {
  const input = JSON.parse(readFileSync(0, 'utf8') || '{}');
  const projectDir = process.env.CLAUDE_PROJECT_DIR || input.cwd || process.cwd();
  const filePath = input.tool_input?.file_path ?? input.tool_input?.notebook_path;
  if (!filePath) process.exit(0);

  const abs = path.resolve(projectDir, filePath);
  const rel = path.relative(projectDir, abs).split(path.sep).join('/');
  const isTestFile = rel.startsWith('backend/tests/') || rel.endsWith('.spec.ts');
  const locked = existsSync(path.join(projectDir, '.claude', 'tests.lock'));

  if (isTestFile && locked) {
    console.error(
      `Blocked by guard-tests hook: "${rel}" is a test file and tests are locked (.claude/tests.lock). ` +
        'Make the implementation satisfy the existing tests. If you believe a test is wrong, stop and ' +
        'explain why to the user instead of editing it.',
    );
    process.exit(2);
  }
  process.exit(0);
} catch {
  process.exit(0); // never break the session because of a malformed payload
}
