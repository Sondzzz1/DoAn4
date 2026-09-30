export const getApiErrorMessage = (error: any, fallback = 'Không thể thực hiện thao tác.') => {
  const status = error?.response?.status;
  const backendMessage = error?.response?.data?.message;
  if (backendMessage) return backendMessage;
  if (status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  if (status === 404) return 'Không tìm thấy dữ liệu.';
  if (status === 409) return 'Dữ liệu đã thay đổi hoặc thao tác xung đột với trạng thái hiện tại.';
  return fallback;
};
