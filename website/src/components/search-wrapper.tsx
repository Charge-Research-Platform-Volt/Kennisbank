import { FuseSearch } from './fuse-search'
import { docsSource, guideSource } from '@/lib/source'

export function SearchWrapper() {
  // Generate search data on the server
  const searchData = [
    ...docsSource.getPages().map(page => ({
      title: page.data.title || '',
      description: page.data.description || '',
      url: page.url,
      type: 'docs' as const
    })),
    ...guideSource.getPages().map(page => ({
      title: page.data.title || '',
      description: page.data.description || '',
      url: page.url,
      type: 'guide' as const
    }))
  ]

  // Pass the data to the client component
  return <FuseSearch searchData={searchData} />
}