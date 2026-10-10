// Connects the TypeScript SDK's client to the C# server of ../Program.cs over stdio, lists its tools and calls each.
// Client.callTool validates structuredContent against the outputSchema that listTools returned.
import { Client } from '@modelcontextprotocol/sdk/client/index.js';
import { StdioClientTransport } from '@modelcontextprotocol/sdk/client/stdio.js';
import { readFileSync } from 'node:fs';

const sdkVersion = JSON.parse(readFileSync(new URL('./node_modules/@modelcontextprotocol/sdk/package.json', import.meta.url))).version;

const [dll, ...serverArgs] = process.argv.slice(2);
const transport = new StdioClientTransport({ command: 'dotnet', args: [dll, '--stdio', ...serverArgs] });
let negotiated;
transport.setProtocolVersion = (version) => { negotiated = version; };

const client = new Client({ name: 'repro', version: '1.0.0' });
await client.connect(transport);
console.log(`== @modelcontextprotocol/sdk ${sdkVersion} Client over stdio, server args [--stdio${serverArgs.map((a) => ' ' + a).join('')}] (negotiated ${negotiated})`);

const { tools } = await client.listTools();
for (const tool of tools.sort((a, b) => a.name.localeCompare(b.name))) {
  try {
    const result = await client.callTool({ name: tool.name });
    console.log(`tools/call  ${tool.name.padEnd(12)} OK    ${JSON.stringify(result.structuredContent)}`);
  } catch (error) {
    console.log(`tools/call  ${tool.name.padEnd(12)} ERROR ${error.message}`);
  }
}

await client.close();
