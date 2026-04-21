type Env = {
  VITE_API_URL?: string
  VITE_API_BASE_URL?: string
}

export const onRequest: PagesFunction<Env> = async ({ env }) => {
  const payload: Record<string, string> = {}
  if (env.VITE_API_URL) payload.VITE_API_URL = env.VITE_API_URL
  if (env.VITE_API_BASE_URL) payload.VITE_API_BASE_URL = env.VITE_API_BASE_URL

  const body = `window.__ENV__ = ${JSON.stringify(payload)};`

  return new Response(body, {
    headers: {
      'content-type': 'application/javascript; charset=utf-8',
      'cache-control': 'no-store, max-age=0',
    },
  })
}
