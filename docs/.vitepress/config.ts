import { defineConfig } from 'vitepress'

export default defineConfig({
  title: 'MonadicSharp.Recovery',
  description: 'Self-healing extension for MonadicSharp — RescueAsync and StartFixBranchAsync for Railway-Oriented error recovery.',
  base: '/MonadicSharp.Recovery/',
  cleanUrls: true,

  head: [
    ['meta', { property: 'og:type', content: 'website' }],
    ['meta', { name: 'twitter:card', content: 'summary' }],
  ],

  themeConfig: {
    logo: '/logo.svg',
    siteTitle: 'MonadicSharp.Recovery',

    nav: [
      {
        text: 'Guide',
        items: [
          { text: 'Getting Started', link: '/getting-started' },
          { text: 'The Three Lanes', link: '/three-lanes' },
        ],
      },
      {
        text: 'API',
        items: [
          { text: 'RescueAsync', link: '/api/rescue-async' },
          { text: 'StartFixBranchAsync', link: '/api/start-fix-branch-async' },
          { text: 'IRecoveryTelemetry', link: '/api/telemetry' },
        ],
      },
      {
        text: 'Ecosystem',
        items: [
          { text: 'MonadicSharp Core', link: 'https://danny4897.github.io/MonadicSharp/' },
          { text: 'NuGet', link: 'https://www.nuget.org/packages/MonadicSharp.Recovery' },
        ],
      },
    ],

    sidebar: {
      '/': [
        {
          text: 'Guide',
          items: [
            { text: 'Getting Started', link: '/getting-started' },
            { text: 'The Three Lanes', link: '/three-lanes' },
          ],
        },
        {
          text: 'API Reference',
          items: [
            { text: 'RescueAsync', link: '/api/rescue-async' },
            { text: 'StartFixBranchAsync', link: '/api/start-fix-branch-async' },
            { text: 'IRecoveryTelemetry', link: '/api/telemetry' },
          ],
        },
      ],
    },

    socialLinks: [
      { icon: 'github', link: 'https://github.com/Danny4897/MonadicSharp.Recovery' },
    ],

    search: { provider: 'local' },

    footer: {
      message: 'Released under the MIT License.',
      copyright: 'Copyright © 2024–2026 Danny4897',
    },

    outline: { level: [2, 3], label: 'On this page' },
  },

  markdown: {
    theme: { light: 'github-light', dark: 'one-dark-pro' },
  },
})
