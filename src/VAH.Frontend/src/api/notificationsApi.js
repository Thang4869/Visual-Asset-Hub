import apiClient from './client';

export const getNotifications = (page = 1, pageSize = 10) => {
    return apiClient.get('/notifications', { params: { page, pageSize } })
        .then(res => res.data);
};

export const markAsRead = (id) => {
    return apiClient.put(`/notifications/${id}/read`).then(res => res.data);
};
