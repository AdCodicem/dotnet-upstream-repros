// Connects the TypeScript SDK's client to the C# server of ../Program.cs over stdio, lists its tools and calls each.
// The client asks for its latest protocol version and accepts what the server answers.
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import { ResultSchema } from '@modelcontextprotocol/sdk/types.js';
import { readFileSync } from 'node:fs';

const sdkVersion = JSON.parse(readFileSync(new URL('./node_modules/@modelcontextprotocol/sdk/package.json', import.meta.url))).version;

const [dll, ...serverArgs] = process.argv.slice(2);
const transport = new StdioClientTransport({ command: 'dotnet', args: [dll, '--stdio', ...serverArgs] });
let negotiated;
transport.setProtocolVersion = (version) => { negotiated = version; };

const client = new Client({ name: 'repro', version: '1.0.0' });
await client.connect(transport);
console.log(`== @modelcontextprotocol/sdk ${sdkVersion} Client over stdio, server args [--stdio${serverArgs.map((a) => ' ' + a).join('')}] (negotiated ${negotiated})`);

let tools = [];
try {
  ({ tools } = await client.listTools());
} catch (error) {
  let message = error.message;
  try { message = JSON.stringify(JSON.parse(message)); } catch { /* not JSON: keep the text */ }
  console.log(`ERROR from listTools: ${message}`);
  // The same request, read without the SDK's ListToolsResult schema, to show what the server sent.
  const raw = await client.request({ method: 'tools/list', params: {} }, ResultSchema);
  raw.tools.forEach((tool, index) => console.log(`raw tools/list  tools[${index}] ${tool.name.padEnd(13)} outputSchema: ${JSON.stringify(tool.outputSchema)}`));
}

for (const tool of tools.sort((a, b) => a.name.localeCompare(b.name))) {
  console.log(`tools/list  ${tool.name.padEnd(13)} outputSchema:      ${JSON.stringify(tool.outputSchema)}`);
  const result = await client.callTool({ name: tool.name });
  console.log(`tools/call  ${tool.name.padEnd(13)} structuredContent: ${JSON.stringify(result.structuredContent)}`);
}

await client.close();
