import { useEffect, useMemo } from 'react';

export default function Toast({
  message,
  type,
  onClose,
}: {
  message: string;
  type: 'info' | 'error' | 'success' | 'warning';
  onClose: () => void;
}) {
  useEffect(() => {
    const timer = setTimeout(() => {
      onClose();
    }, 3000); // Auto-dismiss after 3 seconds

    return () => clearTimeout(timer); // Cleanup timer on unmount
  }, [onClose]);

  const alertStyle = useMemo(() => {
    switch (type) {
      case 'error':
        return 'alert-error';
      case 'success':
        return 'alert-success';
      case 'warning':
        return 'alert-warning';
      case 'info':
        return 'alert-info';
    }
  }, [type]);

  return (
    <div className="toast toast-top top-20 z-50 animate-fade shadow-lg">
      <div className={`alert ${alertStyle}`}>
        <span>{message}</span>
      </div>
    </div>
  );
}
