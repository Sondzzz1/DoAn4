type UnknownRecord = Record<string, unknown>;

const asRecord = (value: unknown): UnknownRecord | undefined => (
  typeof value === 'object' && value !== null ? value as UnknownRecord : undefined
);

export const getApiErrorMessage = (error: unknown, fallback = 'Không thể thực hiện thao tác.') => {
  const response = asRecord(asRecord(error)?.response);
  const data = asRecord(response?.data);
  const status = response?.status;
  const backendMessage = data?.message;

  if (typeof backendMessage === 'string' && backendMessage.trim()) return backendMessage;
  if (status === 403) return 'Bạn không có quyền thực hiện thao tác này.';
  if (status === 404) return 'Không tìm thấy dữ liệu.';
  if (status === 409) return 'Dữ liệu đã thay đổi hoặc thao tác xung đột với trạng thái hiện tại.';
  return fallback;
};
