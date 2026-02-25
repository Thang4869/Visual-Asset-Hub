import React from 'react';

/**
 * React Error Boundary that catches JS errors in the child component tree.
 * Shows a friendly fallback UI instead of a blank screen.
 */
class ErrorBoundary extends React.Component {
  constructor(props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error) {
    return { hasError: true, error };
  }

  componentDidCatch(error, info) {
    console.error('[ErrorBoundary]', error, info.componentStack);
  }

  handleReset = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      return (
        <div style={{
          display: 'flex', flexDirection: 'column', alignItems: 'center',
          justifyContent: 'center', height: '100vh',
          background: '#0a1929', color: '#b0bec5', fontFamily: 'Inter, sans-serif',
        }}>
          <h2 style={{ color: '#ef5350', marginBottom: 8 }}>Đã xảy ra lỗi</h2>
          <p style={{ maxWidth: 480, textAlign: 'center', marginBottom: 16 }}>
            Ứng dụng gặp sự cố không mong muốn. Vui lòng thử lại.
          </p>
          <button
            onClick={this.handleReset}
            style={{
              padding: '8px 24px', borderRadius: 6, border: 'none',
              background: '#1976d2', color: '#fff', cursor: 'pointer', fontSize: 14,
            }}
          >
            Thử lại
          </button>
        </div>
      );
    }
    return this.props.children;
  }
}

export default ErrorBoundary;
