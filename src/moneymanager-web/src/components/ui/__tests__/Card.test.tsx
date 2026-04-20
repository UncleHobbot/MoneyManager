import { render, screen } from '@testing-library/react'
import { Card } from '../Card'

describe('Card', () => {
  it('renders title, subtitle, and children', () => {
    render(
      <Card title="Summary" subtitle="Details">
        <div>Body content</div>
      </Card>,
    )

    expect(screen.getByText('Summary')).toBeInTheDocument()
    expect(screen.getByText('Details')).toBeInTheDocument()
    expect(screen.getByText('Body content')).toBeInTheDocument()
  })

  it('applies custom body classes to the body wrapper', () => {
    render(
      <Card bodyClassName="flex min-h-0 flex-1">
        <div>Scrollable content</div>
      </Card>,
    )

    expect(screen.getByText('Scrollable content').parentElement).toHaveClass('flex', 'min-h-0', 'flex-1')
  })
})
