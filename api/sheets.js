const SCRIPT_URL = 'https://script.google.com/macros/s/AKfycbyfL4O5uSARYgDsdQTLzhRQQOQGiFtxExmN1RLYZKUSQfSNJBGu6v797ByBQ7-ErFBp/exec';

export default async function handler(req, res) {
  res.setHeader('Access-Control-Allow-Origin', '*');
  res.setHeader('Access-Control-Allow-Methods', 'POST, OPTIONS');
  res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

  if (req.method === 'OPTIONS') {
    return res.status(200).end();
  }

  if (req.method !== 'POST') {
    return res.status(405).json({ error: 'Method not allowed' });
  }

  try {
    const payload = req.body;

    const response = await fetch(SCRIPT_URL, {
      method: 'POST',
      headers: { 'Content-Type': 'text/plain' },
      body: JSON.stringify(payload),
      redirect: 'follow',
    });

    const text = await response.text();
    let result;
    try { result = JSON.parse(text); }
    catch { result = { status: 'ok' }; }

    return res.status(200).json(result);
  } catch (err) {
    return res.status(500).json({ status: 'error', message: err.message });
  }
}
